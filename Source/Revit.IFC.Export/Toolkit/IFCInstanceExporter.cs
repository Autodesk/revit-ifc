//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
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
using Revit.IFC.Export.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Export.Toolkit
{
   public class IFCInstanceExporter
   {
      /////////////////////////////////////////////////////////////////
      // SetXXX method is to set base entities' attributes.
      // Every SetXXX method has a corresponding ValidateXXX method.
      // ValidateXXX is to validate the parameters for the attributes.
      // ValidateXXX should not be called in SetXXX. It must be called in CreateXXX method.
      // This is to make sure all arguments are valid BEFORE create an instance.
      // So we have below layout for these methods:
      //   ValidateABCBaseEntity(...) { ... }
      //   SetABCBaseEntity(...) { ... }
      //   CreateABCEntity(...)
      //      {
      //         //Code to validate ABC entity's own parameters goes here.
      //         ValidateABCBaseEntity(...);
      //         //Code to create ABC instance goes here.
      //         //Code to set ABC entity's own attributes goes here.
      //         SetABCBaseEntity(...);
      //      }
      ///////////////////////////////////////////////////////////////

      private static IFCAnyHandle CreateInstance(IFCFile file, IFCEntityType type, Element element)
      {
         IFCAnyHandle hnd = IFCAnyHandleUtil.CreateInstance(file, type);

         // Set the IfcRoot Name and Description override here to make it consistent accross
         if (IFCAnyHandleUtil.IsSubTypeOf(hnd,IFCEntityType.IfcRoot) && element != null)
         {
            string nameOverride = NamingUtil.GetNameOverride(element, null);
            if (!string.IsNullOrEmpty(nameOverride))
               IFCAnyHandleUtil.SetAttribute(hnd, "Name", nameOverride);

            string descOverride = NamingUtil.GetDescriptionOverride(element, null);
            if (!string.IsNullOrEmpty(descOverride))
               IFCAnyHandleUtil.SetAttribute(hnd, "Description", descOverride);
         }
         return hnd;
      }

      #region private validation and set methods goes here

      /// <summary>
      /// To validate that the Entity Type string is a valid type and further check that it is valid within the specific schema version selected
      /// </summary>
      /// <param name="entityTypeStr">IFC entity type in string format</param>
      /// <returns>The handle, or null.</returns>
      private static string ValidateEntityTypeStr(string entityTypeStr)
      {
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            Revit.IFC.Common.Enums.IFC4.IFCEntityType entityTypeEnum;
            if (Enum.TryParse(entityTypeStr, true, out entityTypeEnum))     //check for valid IFC4 entity type
               return entityTypeStr;                                       //if valid, return the original type str
         }
         else
         {
            Revit.IFC.Common.Enums.IFC2x.IFCEntityType entityTypeEnum;
            if (!Enum.TryParse(entityTypeStr, true, out entityTypeEnum))    //check for valid IFC2x- entity type
            {
               IFCEntityType entTypeTest, entTypeUse;
               if (Enum.TryParse(entityTypeStr, true, out entTypeTest))    //check for valid IFC entity type (combined)
               {
                  if (IFCCompatibilityType.checkCompatibleType(entTypeTest, out entTypeUse))  //check whether it is MEP type that needs to create the supertype in IFC2x-
                     return entTypeUse.ToString();
                  else
                     return entityTypeStr;
               }
            }
         }
         throw new ArgumentException("Entity string is invalid", entityTypeStr);
      }

      /// <summary>
      /// Validates the values to be set to IfcRoot.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      private static void ValidateRoot(string guid, IFCAnyHandle ownerHistory)
      {
         if (String.IsNullOrEmpty(guid))
            throw new ArgumentException("Invalid guid.", "guid");

         IFCAnyHandleUtil.ValidateSubTypeOf(ownerHistory, false, IFCEntityType.IfcOwnerHistory);
      }

      private static void SetRoot(IFCAnyHandle root, Element element, string guid, IFCAnyHandle ownerHistory)
      {
         string name = NamingUtil.GetNameOverride(root, element, NamingUtil.GetIFCName(element));
         string description = NamingUtil.GetDescriptionOverride(root, element, null);
         SetRoot(root, guid, ownerHistory, name, description);
      }
      /// <summary>
      /// Sets attributes to IfcRoot.
      /// </summary>
      /// <param name="root">The IfcRoot.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      private static void SetRoot(IFCAnyHandle root, string guid, IFCAnyHandle ownerHistory, string name, string description)
      {
         ExporterUtil.SetGlobalId(root, guid);
         IFCAnyHandleUtil.SetAttribute(root, "OwnerHistory", ownerHistory);
         setRootName(root, name);
         setRootDescription(root, description);
      }
      private static void setRootName(IFCAnyHandle root, string name)
      {
         if (name != null)
            IFCAnyHandleUtil.SetAttribute(root, "Name", name);
      }
      private static void setRootDescription(IFCAnyHandle root, string description)
      {
         if (description != null)
            IFCAnyHandleUtil.SetAttribute(root, "Description", description);
      }

      /// <summary>
      /// Validates the values to be set to IfcObjectDefinition.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      private static void ValidateObjectDefinition(string guid, IFCAnyHandle ownerHistory)
      {
         ValidateRoot(guid, ownerHistory);
      }

      /// <summary>
      /// Sets attributes to IfcObjectDefinition.
      /// </summary>
      /// <param name="objectDefinition">The IfcObjectDefinition.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
	  private static void SetObjectDefinition(IFCAnyHandle objectDefinition, string guid, IFCAnyHandle ownerHistory, string name, string description)
      {

         SetRoot(objectDefinition, guid, ownerHistory, name, description);
      }
      private static void SetObjectDefinition(IFCAnyHandle objectDefinition, Element element, string guid, IFCAnyHandle ownerHistory)
      {
         SetRoot(objectDefinition, element, guid, ownerHistory);
      }

      /// <summary>
      /// Validates the values to be set to IfcTypeObject.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      private static void ValidateTypeObject(string guid, IFCAnyHandle ownerHistory, HashSet<IFCAnyHandle> propertySets)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(propertySets, true, IFCEntityType.IfcPropertySetDefinition);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
            ValidatePropertyDefinition(guid, ownerHistory);
         else
            ValidateObjectDefinition(guid, ownerHistory);
      }

      /// <summary>
      /// Sets attributes to IfcTypeObject.
      /// </summary>
      /// <param name="typeObject">The IfcTypeObject.</param>
      /// <param name="revitType">The Revit element.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      private static void SetTypeObject(IFCAnyHandle typeObject, Element revitType, HashSet<IFCAnyHandle> propertySets)
      {
         string guid = null;
         string applicableOccurrence = null;
         if (revitType != null)
         {
            guid = GUIDUtil.CreateGUID(revitType);
            applicableOccurrence = NamingUtil.GetOverrideStringValue(revitType, "IfcApplicableOccurrence", null);
         }
         else
            guid = GUIDUtil.CreateGUID();

         if (!string.IsNullOrEmpty(applicableOccurrence))
            IFCAnyHandleUtil.SetAttribute(typeObject, "ApplicableOccurrence", applicableOccurrence);
         if (propertySets != null && propertySets.Count > 0)
            IFCAnyHandleUtil.SetAttribute(typeObject, "HasPropertySets", propertySets);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
            SetPropertyDefinition(typeObject, revitType, guid, ExporterCacheManager.OwnerHistoryHandle);
         else
            SetObjectDefinition(typeObject, revitType, guid, ExporterCacheManager.OwnerHistoryHandle);
      }

      /// <summary>
      /// Validates the values to be set to IfcTypeProduct.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      private static void ValidateTypeProduct(string guid, IFCAnyHandle ownerHistory, HashSet<IFCAnyHandle> propertySets,
          IList<IFCAnyHandle> representationMaps)
      {
         //elementTag can be optional
         IFCAnyHandleUtil.ValidateSubTypeOf(representationMaps, true, IFCEntityType.IfcRepresentationMap);

         ValidateTypeObject(guid, ownerHistory, propertySets);
      }

      /// <summary>
      /// Sets attributes to IfcTypeProduct.
      /// </summary>
      /// <param name="typeProduct">The IfcTypeProduct.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      private static void SetTypeProduct(IFCAnyHandle typeProduct, Element revitType, HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps)
      {
         if (representationMaps != null && representationMaps.Count > 0)
            IFCAnyHandleUtil.SetAttribute(typeProduct, "RepresentationMaps", representationMaps);

         string tag = NamingUtil.GetTagOverride(revitType, NamingUtil.CreateIFCElementId(revitType));
         IFCAnyHandleUtil.SetAttribute(typeProduct, "Tag", tag);

         SetTypeObject(typeProduct, revitType, propertySets);
      }

      /// <summary>
      /// Validates the values to be set to IfcElementType.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      private static void ValidateElementType(IFCAnyHandle ownerHistory, string name, HashSet<IFCAnyHandle> propertySets,
          IList<IFCAnyHandle> representationMaps)
      {
         ValidateTypeProduct("null", ownerHistory, propertySets, representationMaps);
      }

      /// <summary>
      /// Sets attributes to IfcElementType.
      /// </summary>
      /// <param name="elementType">The IfcElementType.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      private static void SetElementType(IFCAnyHandle elementType, Element revitType, HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps)
      {

         ValidateElementType(ExporterCacheManager.OwnerHistoryHandle, NamingUtil.GetNameOverride(elementType, revitType, NamingUtil.GetIFCName(revitType)), propertySets, representationMaps);
         string type = NamingUtil.GetOverrideStringValue(revitType, "IfcElementType", null);
         IFCAnyHandleUtil.SetAttribute(elementType, "ElementType", type);

         SetTypeProduct(elementType, revitType, propertySets, representationMaps);
      }

      /// <summary>
      /// Validates the values to be set to IfcPropertyDefinition.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      private static void ValidatePropertyDefinition(string guid, IFCAnyHandle ownerHistory)
      {
         ValidateRoot(guid, ownerHistory);
      }

      /// <summary>
      /// Sets attributes to IfcPropertyDefinition.
      /// </summary>
      /// <param name="propertyDefinition">The IfcPropertyDefinition.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      private static void SetPropertyDefinition(IFCAnyHandle propertyDefinition, string guid, IFCAnyHandle ownerHistory, string name, string description)
      {
         SetRoot(propertyDefinition, guid, ownerHistory, name, description);
      }

      private static void SetPropertyDefinition(IFCAnyHandle propertyDefinition, Element element, string guid, IFCAnyHandle ownerHistory)
      {
         SetRoot(propertyDefinition, element, guid, ownerHistory);
      }

      /// <summary>
      /// Validates the values to be set to IfcRelationship.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      private static void ValidateRelationship(string guid, IFCAnyHandle ownerHistory)
      {
         ValidateRoot(guid, ownerHistory);
      }

      /// <summary>
      /// Sets attributes to IfcRelationship.
      /// </summary>
      /// <param name="relationship">The IfcRelationship.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      private static void SetRelationship(IFCAnyHandle relationship,
          string guid, IFCAnyHandle ownerHistory, string name, string description)
      {
         SetRoot(relationship, guid, ownerHistory, name, description);
      }

      /// <summary>
      /// Validates the values to be set to IfcPropertySetDefinition.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      private static void ValidatePropertySetDefinition(string guid, IFCAnyHandle ownerHistory)
      {
         ValidatePropertyDefinition(guid, ownerHistory);
      }

      /// <summary>
      /// Sets attributes to IfcPropertySetDefinition.
      /// </summary>
      /// <param name="propertySetDefinition">The IfcPropertySetDefinition.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      private static void SetPropertySetDefinition(IFCAnyHandle propertySetDefinition,
          string guid, IFCAnyHandle ownerHistory, string name, string description)
      {
         SetPropertyDefinition(propertySetDefinition, guid, ownerHistory, name, description);
      }

      /// <summary>
      /// Validates the values to be set to IfcRelAssociates.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">The objects to be related to the material.</param>
      private static void ValidateRelAssociates(string guid, IFCAnyHandle ownerHistory, string name, string description, HashSet<IFCAnyHandle> relatedObjects)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(relatedObjects, false, IFCEntityType.IfcRoot);

         ValidateRelationship(guid, ownerHistory);
      }

      /// <summary>
      /// Sets attributes to IfcRelAssociates.
      /// </summary>
      /// <param name="relAssociates">The IfcRelAssociates.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">The objects to be related to the material.</param>
      private static void SetRelAssociates(IFCAnyHandle relAssociates,
          string guid, IFCAnyHandle ownerHistory, string name, string description, HashSet<IFCAnyHandle> relatedObjects)
      {
         IFCAnyHandleUtil.SetAttribute(relAssociates, "RelatedObjects", relatedObjects);
         SetRelationship(relAssociates, guid, ownerHistory, name, description);
      }

      /// <summary>
      /// Validates the values to be set to IfcRelDefines.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">The objects to be related to a type.</param>
      private static void ValidateRelDefines(string guid, IFCAnyHandle ownerHistory, string name, string description, ICollection<IFCAnyHandle> relatedObjects)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(relatedObjects, false, IFCEntityType.IfcObject);

         ValidateRelationship(guid, ownerHistory);
      }

      /// <summary>
      /// Validates the values to be set to IfcRelDefines.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">The objects to be related to a type.</param>
      private static void ValidateRelDefinesByPropertiesIFC4(ICollection<IFCAnyHandle> relatedObjects)
      {
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4 && (relatedObjects == null || relatedObjects.Count != 1))
            throw new ArgumentException("relatedObjects can only have 1 entry fo IFC4.");
      }

      /// <summary>
      /// Sets attributes to IfcRelDefines.
      /// </summary>
      /// <param name="relDefines">The IfcRelDefines.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">The objects to be related to a type.</param>
      private static void SetRelDefines(IFCAnyHandle relDefines,
          string guid, IFCAnyHandle ownerHistory, string name, string description, ISet<IFCAnyHandle> relatedObjects)
      {
         IFCAnyHandleUtil.SetAttribute(relDefines, "RelatedObjects", relatedObjects);
         SetRelationship(relDefines, guid, ownerHistory, name, description);
      }

      /// <summary>
      /// Validates the values to be set to IfcRelDecomposes.
      /// </summary>
      /// <param name="guid">The GUID for the entity.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatingObject">The element to which the structure contributes.</param>
      /// <param name="relatedObjects">The elements that make up the structure.</param>
      private static void ValidateRelDecomposes(string guid, IFCAnyHandle ownerHistory, string name, string description,
          IFCAnyHandle relatingObject, HashSet<IFCAnyHandle> relatedObjects)
      {
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
         {
            IFCAnyHandleUtil.ValidateSubTypeOf(relatingObject, false, IFCEntityType.IfcObject);

            IFCAnyHandleUtil.ValidateSubTypeOf(relatedObjects, false, IFCEntityType.IfcObject);
         }
         else
         {
            IFCAnyHandleUtil.ValidateSubTypeOf(relatingObject, false, IFCEntityType.IfcObjectDefinition);
            IFCAnyHandleUtil.ValidateSubTypeOf(relatedObjects, false, IFCEntityType.IfcObjectDefinition);
         }

         ValidateRelationship(guid, ownerHistory);
      }

      /// <summary>
      /// Sets attributes to IfcRelDecomposes.
      /// </summary>
      /// <param name="relDecomposes">The IfcRelDecomposes.</param>
      /// <param name="guid">The GUID for the entity.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatingObject">The element to which the structure contributes.</param>
      /// <param name="relatedObjects">The elements that make up the structure.</param>
      private static void SetRelDecomposes(IFCAnyHandle relDecomposes,
          string guid, IFCAnyHandle ownerHistory, string name, string description,
          IFCAnyHandle relatingObject, HashSet<IFCAnyHandle> relatedObjects)
      {
         IFCAnyHandleUtil.SetAttribute(relDecomposes, "RelatingObject", relatingObject);
         IFCAnyHandleUtil.SetAttribute(relDecomposes, "RelatedObjects", relatedObjects);
         SetRelationship(relDecomposes, guid, ownerHistory, name, description);
      }

      /// <summary>
      /// Validates the values to be set to IfcRelConnects.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      private static void ValidateRelConnects(string guid, IFCAnyHandle ownerHistory, string name)
      {
         ValidateRelationship(guid, ownerHistory);
      }

      /// <summary>
      /// Sets attributes to IfcRelConnects.
      /// </summary>
      /// <param name="relConnects">The IfcRelConnects.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      private static void SetRelConnects(IFCAnyHandle relConnects,
          string guid, IFCAnyHandle ownerHistory, string name, string description)
      {
         SetRelationship(relConnects, guid, ownerHistory, name, description);
      }

      /// <summary>
      /// Validates the values to be set to IfcObject.
      /// </summary>
      /// <param name="guid">The GUID to use to label the wall.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      private static void ValidateObject(string guid, IFCAnyHandle ownerHistory)
      {
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
            ValidateRoot(guid, ownerHistory);
         else
            ValidateObjectDefinition(guid, ownerHistory);
      }

      /// <summary>
      /// Sets attributes to IfcObject.
      /// </summary>
      /// <param name="obj">The IfcObject.</param>
      /// <param name="guid">The GUID to use to label the wall.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      private static void SetObject(IFCAnyHandle obj, string guid, IFCAnyHandle ownerHistory, string name, string description, string objectType)
      {
         IFCAnyHandleUtil.SetAttribute(obj, "ObjectType", objectType);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
            SetRoot(obj, guid, ownerHistory, name, description);
         else
            SetObjectDefinition(obj, guid, ownerHistory, name, description);
      }

      private static void SetObject(ExporterIFC exporterIFC, IFCAnyHandle obj, Element element, string guid, IFCAnyHandle ownerHistory)
      {
         string objectType = NamingUtil.GetObjectTypeOverride(obj, element, NamingUtil.GetFamilyAndTypeName(element));
         IFCAnyHandleUtil.SetAttribute(obj, "ObjectType", objectType);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
            SetRoot(obj, element, guid, ownerHistory);
         else
            SetObjectDefinition(obj, element, guid, ownerHistory);
      }

      /// <summary>
      /// Validates the values to be set to IfcProduct.
      /// </summary>
      /// <param name="guid">The GUID to use to label the wall.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The representation object assigned to the wall.</param>
      private static void ValidateProduct(string guid, IFCAnyHandle ownerHistory, IFCAnyHandle objectPlacement, IFCAnyHandle representation)
      {
         //objectPlacement can be optional
         IFCAnyHandleUtil.ValidateSubTypeOf(objectPlacement, true, IFCEntityType.IfcLocalPlacement);

         //representation can be optional
         IFCAnyHandleUtil.ValidateSubTypeOf(representation, true, IFCEntityType.IfcProductRepresentation);

         ValidateObject(guid, ownerHistory);
      }

      /// <summary>
      /// Sets attributes to IfcProduct.
      /// </summary>
      /// <param name="product">The IfcProduct.</param>
      /// <param name="guid">The GUID to use to label the wall.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The representation object assigned to the wall.</param>

      private static void SetProduct(ExporterIFC exporterIFC, IFCAnyHandle product, Element element, string guid, IFCAnyHandle ownerHistory, IFCAnyHandle objectPlacement, IFCAnyHandle representation)
      {
         IFCAnyHandleUtil.SetAttribute(product, "ObjectPlacement", objectPlacement);
         IFCAnyHandleUtil.SetAttribute(product, "Representation", representation);
         SetObject(exporterIFC, product, element, guid, ownerHistory);
      }

      /// <summary>
      /// Validates the values to be set to IfcGroup.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      private static void ValidateGroup(string guid, IFCAnyHandle ownerHistory, string name, string objectType)
      {
         ValidateObject(guid, ownerHistory);
      }

      /// <summary>
      /// Sets attributes to IfcGroup.
      /// </summary>
      /// <param name="group">The IfcGroup.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      private static void SetGroup(IFCAnyHandle group,
          string guid, IFCAnyHandle ownerHistory, string name, string description, string objectType)
      {
         SetObject(group, guid, ownerHistory, name, description, objectType);
      }

      /// <summary>
      /// Validates the values to be set to IfcSystem.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      private static void ValidateSystem(string guid, IFCAnyHandle ownerHistory, string name, string objectType)
      {
         ValidateGroup(guid, ownerHistory, name, objectType);
      }

      /// <summary>
      /// Sets attributes to IfcSystem.
      /// </summary>
      /// <param name="system">The IfcSystem.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      private static void SetSystem(IFCAnyHandle system,
          string guid, IFCAnyHandle ownerHistory, string name, string description, string objectType)
      {
         SetGroup(system, guid, ownerHistory, name, description, objectType);
      }

      /// <summary>
      /// Validates the values to be set to IfcElement.
      /// </summary>
      /// <param name="guid">The GUID to use to label the wall.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The representation object assigned to the wall.</param>
      /// <param name="elementTag">The tag for the identifier of the element.</param>
      private static void ValidateElement(string guid, IFCAnyHandle ownerHistory, IFCAnyHandle objectPlacement, IFCAnyHandle representation)
      {
         ValidateProduct(guid, ownerHistory, objectPlacement, representation);
      }

      /// <summary>
      /// Sets attributes to IfcElement.
      /// </summary>
      /// <param name="element">The IfcElement.</param>
      /// <param name="revitElement">The Revit element.</param>
      /// <param name="guid">The GUID to use to label the wall.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The representation object assigned to the wall.</param>
      /// <param name="allowTag">Optional parameter; if false, don't create the tag.</param>
      private static void SetElement(ExporterIFC exporterIFC, IFCAnyHandle element, Element revitElement,
          string guid, IFCAnyHandle ownerHistory, IFCAnyHandle objectPlacement, IFCAnyHandle representation,
          bool allowTag = true)
      {
         SetProduct(exporterIFC, element, revitElement, guid, ownerHistory, objectPlacement, representation);

         if (allowTag && revitElement != null)
         {
            string elementTag = NamingUtil.GetTagOverride(revitElement, NamingUtil.CreateIFCElementId(revitElement));
            try
            {
               IFCAnyHandleUtil.SetAttribute(element, "Tag", elementTag);
            }
            catch
            {
            }
         }
      }

      /// <summary>
      /// Validates the values to be set to IfcSpatialStructureElement.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The representation object.</param>
      /// <param name="longName">The long name.</param>
      /// <param name="compositionType">The composition type.</param>
      private static void ValidateSpatialStructureElement(string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, IFCElementComposition compositionType)
      {
         ValidateProduct(guid, ownerHistory, objectPlacement, representation);
      }

      /// <summary>
      /// Sets attributes to IfcSpatialStructureElement.
      /// </summary>
      /// <param name="spatialStructureElement">The IfcSpatialStructureElement.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The representation object.</param>
      /// <param name="longName">The long name.</param>
      /// <param name="compositionType">The composition type.</param>
      private static void SetSpatialStructureElement(ExporterIFC exporterIFC, IFCAnyHandle spatialStructureElement, Element element,
          string guid, IFCAnyHandle ownerHistory, string name, string description,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string longName, IFCElementComposition compositionType)
      {

         IFCAnyHandleUtil.SetAttribute(spatialStructureElement, "LongName", longName);
         IFCAnyHandleUtil.SetAttribute(spatialStructureElement, "CompositionType", compositionType);
         SetProduct(exporterIFC, spatialStructureElement, element, guid, ownerHistory, objectPlacement, representation);
         setRootName(spatialStructureElement, name);
         setRootDescription(spatialStructureElement, description);
      }

      /// <summary>
      /// Validates the values to be set to IfcRelConnectsElements.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="connectionGeometry">The geometric shape representation of the connection geometry.</param>
      /// <param name="relatingElement">Reference to a subtype of IfcElement that is connected by the connection relationship in the role of RelatingElement.</param>
      /// <param name="relatedElement">Reference to a subtype of IfcElement that is connected by the connection relationship in the role of RelatedElement.</param>
      private static void ValidateRelConnectsElements(string guid, IFCAnyHandle ownerHistory,
          string name, IFCAnyHandle connectionGeometry, IFCAnyHandle relatingElement, IFCAnyHandle relatedElement)
      {
         //connectionGeometry can be optional
         IFCAnyHandleUtil.ValidateSubTypeOf(connectionGeometry, true, IFCEntityType.IfcConnectionGeometry);

         IFCAnyHandleUtil.ValidateSubTypeOf(relatingElement, false, IFCEntityType.IfcElement);

         IFCAnyHandleUtil.ValidateSubTypeOf(relatedElement, false, IFCEntityType.IfcElement);

         ValidateRelConnects(guid, ownerHistory, name);
      }

      /// <summary>
      /// Sets attributes to IfcRelConnectsElements.
      /// </summary>
      /// <param name="relConnectsElements">The IfcRelConnectsElements.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="connectionGeometry">The geometric shape representation of the connection geometry.</param>
      /// <param name="relatingElement">Reference to a subtype of IfcElement that is connected by the connection relationship in the role of RelatingElement.</param>
      /// <param name="relatedElement">Reference to a subtype of IfcElement that is connected by the connection relationship in the role of RelatedElement.</param>
      private static void SetRelConnectsElements(IFCAnyHandle relConnectsElements, string guid, IFCAnyHandle ownerHistory,
          string name, string description, IFCAnyHandle connectionGeometry, IFCAnyHandle relatingElement, IFCAnyHandle relatedElement)
      {
         IFCAnyHandleUtil.SetAttribute(relConnectsElements, "ConnectionGeometry", connectionGeometry);
         IFCAnyHandleUtil.SetAttribute(relConnectsElements, "RelatingElement", relatingElement);
         IFCAnyHandleUtil.SetAttribute(relConnectsElements, "RelatedElement", relatedElement);
         SetRelConnects(relConnectsElements, guid, ownerHistory, name, description);
      }

      /// <summary>
      /// Validates the values to be set to IfcRelAssigns.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">Related objects, which are assigned to a single object.</param>
      /// <param name="relatedObjectsType">Particular type of the assignment relationship.</param>
      private static void ValidateRelAssigns(string guid, IFCAnyHandle ownerHistory,
          string name, ICollection<IFCAnyHandle> relatedObjects, IFCObjectType? relatedObjectsType)
      {
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
            IFCAnyHandleUtil.ValidateSubTypeOf(relatedObjects, false, IFCEntityType.IfcObject);
         else
            IFCAnyHandleUtil.ValidateSubTypeOf(relatedObjects, false, IFCEntityType.IfcObjectDefinition);

         ValidateRelationship(guid, ownerHistory);
      }

      /// <summary>
      /// Sets attributes to IfcRelAssigns.
      /// </summary>
      /// <param name="relAssigns">The IfcRelAssigns.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">Related objects, which are assigned to a single object.</param>
      /// <param name="relatedObjectsType">Particular type of the assignment relationship.</param>
      private static void SetRelAssigns(IFCAnyHandle relAssigns, string guid, IFCAnyHandle ownerHistory,
          string name, string description, ISet<IFCAnyHandle> relatedObjects, IFCObjectType? relatedObjectsType)
      {
         IFCAnyHandleUtil.SetAttribute(relAssigns, "RelatedObjects", relatedObjects);
         IFCAnyHandleUtil.SetAttribute(relAssigns, "RelatedObjectsType", relatedObjectsType);
         SetRelationship(relAssigns, guid, ownerHistory, name, description);
      }

      /// <summary>
      /// Validates the values to be set to IfcProductRepresentation.
      /// </summary>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="representations">The collection of representations assigned to the shape.</param>
      private static void ValidateProductRepresentation(string name, string description, IList<IFCAnyHandle> representations)
      {
         //name can be optional
         //description can be optional
         IFCAnyHandleUtil.ValidateSubTypeOf(representations, false, IFCEntityType.IfcRepresentation);
      }

      /// <summary>
      /// Sets attributes to IfcProductRepresentation.
      /// </summary>
      /// <param name="productDefinitionShape">The IfcProductRepresentation.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="representations">The collection of representations assigned to the shape.</param>
      private static void SetProductRepresentation(IFCAnyHandle productDefinitionShape,
          string name, string description, IList<IFCAnyHandle> representations)
      {
         IFCAnyHandleUtil.SetAttribute(productDefinitionShape, "Name", name);
         IFCAnyHandleUtil.SetAttribute(productDefinitionShape, "Description", description);
         IFCAnyHandleUtil.SetAttribute(productDefinitionShape, "Representations", representations);
      }

      /// <summary>
      /// Validates the values to be set to IfcProperty.
      /// </summary>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      private static void ValidateProperty(string name, string description)
      {
         if (name == null)
            throw new ArgumentNullException("name");

         //description can be optional
      }

      /// <summary>
      /// Sets attributes to IfcProperty.
      /// </summary>
      /// <param name="property">The IfcProperty.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      private static void SetProperty(IFCAnyHandle property, string name, string description)
      {
         IFCAnyHandleUtil.SetAttribute(property, "Name", name);
         IFCAnyHandleUtil.SetAttribute(property, "Description", description);
      }

      /// <summary>
      /// Validates the values to be set to IfcRepresentationContext.
      /// </summary>
      /// <param name="identifier">The identifier.</param>
      /// <param name="type">The description of the type of a representation context.</param>
      private static void ValidateRepresentationContext(string identifier, string type)
      {
         //identifier can be optional
         //type can be optional
      }

      /// <summary>
      /// Sets attributes to IfcRepresentationContext.
      /// </summary>
      /// <param name="representationContext">The IfcRepresentationContext.</param>
      /// <param name="identifier">The identifier.</param>
      /// <param name="type">The description of the type of a representation context.</param>
      private static void SetRepresentationContext(IFCAnyHandle representationContext, string identifier, string type)
      {
         IFCAnyHandleUtil.SetAttribute(representationContext, "ContextIdentifier", identifier);
         IFCAnyHandleUtil.SetAttribute(representationContext, "ContextType", type);
      }

      /// <summary>
      /// Validates the values to be set to IfcConnectedFaceSet.
      /// </summary>
      /// <param name="faces">The collection of faces.</param>
      private static void ValidateConnectedFaceSet(HashSet<IFCAnyHandle> faces)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(faces, false, IFCEntityType.IfcFace);
      }

      /// <summary>
      /// Sets attributes to IfcConnectedFaceSet.
      /// </summary>
      /// <param name="connectedFaceSet">The IfcConnectedFaceSet.</param>
      /// <param name="faces">The collection of faces.</param>
      private static void SetConnectedFaceSet(IFCAnyHandle connectedFaceSet, HashSet<IFCAnyHandle> faces)
      {
         IFCAnyHandleUtil.SetAttribute(connectedFaceSet, "CfsFaces", faces);
      }

      /// <summary>
      /// Validates the values to be set to IfcGeometricSet.
      /// </summary>
      /// <param name="geometryElements">The collection of geometric elements.</param>
      private static void ValidateGeometricSet(HashSet<IFCAnyHandle> geometryElements)
      {
         if (geometryElements == null)
            throw new ArgumentNullException("geometryElements");
      }

      /// <summary>
      /// Validates the values to be set to ControlPointsList
      /// </summary>
      /// <param name="controlPointsList">The Control Points List</param>
      private static void ValidateControlPointsList(IList<IList<IFCAnyHandle>> controlPointsList)
      {
         if (controlPointsList == null)
            throw new ArgumentNullException("controlPointsList");
         if (controlPointsList.Count == 0)
            throw new ArgumentNullException("controlPointsList");
         if (controlPointsList.Count == 1 && controlPointsList.FirstOrDefault().Count == 0)
            throw new ArgumentNullException("controlPointsList");

         IFCAnyHandleUtil.ValidateSubTypeOf(controlPointsList.FirstOrDefault().FirstOrDefault(), false, IFCEntityType.IfcCartesianPoint);
      }


      /// <summary>
      /// Validates the values to be set to WeightsData, representing the weights of a NURBS surface.
      /// </summary>
      /// <param name="weightsDataList">The Weights Data List</param>
      private static void ValidateWeightsDataList(IList<IList<double>> weightsDataList)
      {
         if (weightsDataList == null)
            throw new ArgumentNullException("weightsDataList");
         if (weightsDataList.Count == 0)
            throw new ArgumentNullException("weightsDataList");
         if (weightsDataList.Count == 1 && weightsDataList.FirstOrDefault().Count == 0)
            throw new ArgumentNullException("weightsDataList");
      }

      /// <summary>
      /// Validates the values of the List of List of double values
      /// </summary>
      /// <param name="the List">the List</param>
      /// <param name="name">the parameter name of the list</param>
      private static void ValidateListOfList(IList<IList<double>> theList, bool allowNull, string name)
      {
         if (!allowNull)
         {
            if (theList == null)
               throw new ArgumentNullException(name);
            if (theList.Count == 0)
               throw new ArgumentNullException(name);
         }
         if (theList != null)
            if (theList.Count == 1 && theList.FirstOrDefault().Count == 0)
               throw new ArgumentNullException(name);
      }

      /// <summary>
      /// Validates the values of the List of List of int values 
      /// </summary>
      /// <param name="the List">the List</param>
      /// <param name="name">the parameter name of the list</param>
      private static void ValidateListOfList(IList<IList<int>> theList, bool allowNull, string name)
      {
         if (!allowNull)
         {
            if (theList == null)
               throw new ArgumentNullException(name);
            if (theList.Count == 0)
               throw new ArgumentNullException(name);
         }
         if (theList != null)
            if (theList.Count == 1 && theList.FirstOrDefault().Count == 0)
               throw new ArgumentNullException(name);
      }

      /// <summary>
      /// Sets attributes to IfcGeometricSet.
      /// </summary>
      /// <param name="geometricSet">The IfcGeometricSet.</param>
      /// <param name="geometryElements">The collection of geometric elements.</param>
      private static void SetGeometricSet(IFCAnyHandle geometricSet, HashSet<IFCAnyHandle> geometryElements)
      {
         IFCAnyHandleUtil.SetAttribute(geometricSet, "Elements", geometryElements);
      }

      /// <summary>
      /// Validates the values to be set to IfcAddress.
      /// </summary>
      /// <param name="purpose">Identifies the logical location of the address.</param>
      /// <param name="description">Text that relates the nature of the address.</param>
      /// <param name="userDefinedPurpose">Allows for specification of user specific purpose of the address.</param>
      private static void ValidateAddress(IFCAddressType? purpose, string description, string userDefinedPurpose)
      {
         //all can be optional
      }

      /// <summary>
      /// Sets attributes to IfcAddress.
      /// </summary>
      /// <param name="address">The IfcAddress.</param>
      /// <param name="purpose">Identifies the logical location of the address.</param>
      /// <param name="description">Text that relates the nature of the address.</param>
      /// <param name="userDefinedPurpose">Allows for specification of user specific purpose of the address.</param>
      private static void SetAddress(IFCAnyHandle address, IFCAddressType? purpose, string description, string userDefinedPurpose)
      {
         IFCAnyHandleUtil.SetAttribute(address, "Purpose", purpose);
         IFCAnyHandleUtil.SetAttribute(address, "Description", description);
         IFCAnyHandleUtil.SetAttribute(address, "UserDefinedPurpose", userDefinedPurpose);
      }

      /// <summary>
      /// Validates the values to be set to IfcNamedUnit.
      /// </summary>
      /// <param name="dimensions">The dimensions.</param>
      /// <param name="unitType">The type of the unit.</param>
      private static void ValidateNamedUnit(IFCAnyHandle dimensions, IFCUnit unitType)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(dimensions, false, IFCEntityType.IfcDimensionalExponents);
      }

      /// <summary>
      /// Sets attributes to IfcNamedUnit.
      /// </summary>
      /// <param name="namedUnit">The IfcNamedUnit.</param>
      /// <param name="dimensions">The dimensions.</param>
      /// <param name="unitType">The type of the unit.</param>
      private static void SetNamedUnit(IFCAnyHandle namedUnit, IFCAnyHandle dimensions, IFCUnit unitType)
      {
         IFCAnyHandleUtil.SetAttribute(namedUnit, "Dimensions", dimensions);
         IFCAnyHandleUtil.SetAttribute(namedUnit, "UnitType", unitType);
      }

      /// <summary>
      /// Validates the values to be set to IfcPlacement.
      /// </summary>
      /// <param name="location">The origin.</param>
      private static void ValidatePlacement(IFCAnyHandle location)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(location, false, IFCEntityType.IfcCartesianPoint);
      }

      /// <summary>
      /// Sets attributes to IfcPlacement.
      /// </summary>
      /// <param name="placement">The IfcPlacement.</param>
      /// <param name="location">The origin.</param>
      private static void SetPlacement(IFCAnyHandle placement, IFCAnyHandle location)
      {
         IFCAnyHandleUtil.SetAttribute(placement, "Location", location);
      }

      /// <summary>
      /// Validates the values to be set to IfcCartesianTransformationOperator.
      /// </summary>
      /// <param name="axis1">The X direction of the transformation coordinate system.</param>
      /// <param name="axis2">The Y direction of the transformation coordinate system.</param>
      /// <param name="localOrigin">The origin of the transformation coordinate system.</param>
      /// <param name="scale">The scale factor.</param>
      private static void ValidateCartesianTransformationOperator(IFCAnyHandle axis1, IFCAnyHandle axis2,
          IFCAnyHandle localOrigin, double? scale)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(axis1, true, IFCEntityType.IfcDirection);
         IFCAnyHandleUtil.ValidateSubTypeOf(axis2, true, IFCEntityType.IfcDirection);
         IFCAnyHandleUtil.ValidateSubTypeOf(localOrigin, false, IFCEntityType.IfcCartesianPoint);
      }

      /// <summary>
      /// Sets attributes to IfcCartesianTransformationOperator.
      /// </summary>
      /// <param name="cartesianTransformationOperator">The IfcCartesianTransformationOperator.</param>
      /// <param name="axis1">The X direction of the transformation coordinate system.</param>
      /// <param name="axis2">The Y direction of the transformation coordinate system.</param>
      /// <param name="localOrigin">The origin of the transformation coordinate system.</param>
      /// <param name="scale">The scale factor.</param>
      private static void SetCartesianTransformationOperator(IFCAnyHandle cartesianTransformationOperator, IFCAnyHandle axis1,
          IFCAnyHandle axis2, IFCAnyHandle localOrigin, double? scale)
      {
         IFCAnyHandleUtil.SetAttribute(cartesianTransformationOperator, "Axis1", axis1);
         IFCAnyHandleUtil.SetAttribute(cartesianTransformationOperator, "Axis2", axis2);
         IFCAnyHandleUtil.SetAttribute(cartesianTransformationOperator, "LocalOrigin", localOrigin);
         IFCAnyHandleUtil.SetAttribute(cartesianTransformationOperator, "Scale", scale);
      }

      /// <summary>
      /// Validates the values to be set to IfcManifoldSolidBrep.
      /// </summary>
      /// <param name="outer">The closed shell.</param>
      private static void ValidateManifoldSolidBrep(IFCAnyHandle outer)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(outer, false, IFCEntityType.IfcClosedShell);
      }

      /// <summary>
      /// Sets attributes to IfcManifoldSolidBrep.
      /// </summary>
      /// <param name="manifoldSolidBrep">The IfcManifoldSolidBrep.</param>
      /// <param name="outer">The closed shell.</param>
      private static void SetManifoldSolidBrep(IFCAnyHandle manifoldSolidBrep, IFCAnyHandle outer)
      {
         IFCAnyHandleUtil.SetAttribute(manifoldSolidBrep, "Outer", outer);
      }

      /// <summary>
      /// Validates the values to be set to IfcGeometricRepresentationContext.
      /// </summary>
      /// <param name="identifier">The identifier.</param>
      /// <param name="type">The description of the type of a representation context.</param>
      /// <param name="dimension">The integer dimension count of the coordinate space modeled in a geometric representation context.</param>
      /// <param name="precision">Value of the model precision for geometric models.</param>
      /// <param name="worldCoordinateSystem">Establishment of the engineering coordinate system (often referred to as the world coordinate system in CAD)
      /// for all representation contexts used by the project.</param>
      /// <param name="trueNorth">Direction of the true north relative to the underlying coordinate system.</param>
      private static void ValidateGeometricRepresentationContext(string identifier, string type, int dimension,
          double? precision, IFCAnyHandle worldCoordinateSystem, IFCAnyHandle trueNorth)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(worldCoordinateSystem, false, IFCEntityType.IfcAxis2Placement2D, IFCEntityType.IfcAxis2Placement3D);
         IFCAnyHandleUtil.ValidateSubTypeOf(trueNorth, true, IFCEntityType.IfcDirection);
         ValidateRepresentationContext(identifier, type);
      }

      /// <summary>
      /// Sets attributes to IfcGeometricRepresentationContext.
      /// </summary>
      /// <param name="geometricRepresentationContext">The IfcGeometricRepresentationContext.</param>
      /// <param name="identifier">The identifier.</param>
      /// <param name="type">The description of the type of a representation context.</param>
      /// <param name="dimension">The integer dimension count of the coordinate space modeled in a geometric representation context.</param>
      /// <param name="precision">Value of the model precision for geometric models.</param>
      /// <param name="worldCoordinateSystem">Establishment of the engineering coordinate system (often referred to as the world coordinate system in CAD)
      /// for all representation contexts used by the project.</param>
      /// <param name="trueNorth">Direction of the true north relative to the underlying coordinate system.</param>
      private static void SetGeometricRepresentationContext(IFCAnyHandle geometricRepresentationContext,
          string identifier, string type, int dimension, double? precision, IFCAnyHandle worldCoordinateSystem,
          IFCAnyHandle trueNorth)
      {
         IFCAnyHandleUtil.SetAttribute(geometricRepresentationContext, "CoordinateSpaceDimension", dimension);
         IFCAnyHandleUtil.SetAttribute(geometricRepresentationContext, "Precision", precision);
         IFCAnyHandleUtil.SetAttribute(geometricRepresentationContext, "WorldCoordinateSystem", worldCoordinateSystem);
         IFCAnyHandleUtil.SetAttribute(geometricRepresentationContext, "TrueNorth", trueNorth);
         SetRepresentationContext(geometricRepresentationContext, identifier, type);
      }

      /// <summary>
      /// Validates the values to be set to IfcPhysicalQuantity.
      /// </summary>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      private static void ValidatePhysicalQuantity(string name, string description)
      {
         if (name == null)
            throw new ArgumentNullException("name");
      }

      /// <summary>
      /// Sets attributes to IfcPhysicalQuantity.
      /// </summary>
      /// <param name="physicalQuantity">The IfcPhysicalQuantity.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      private static void SetPhysicalQuantity(IFCAnyHandle physicalQuantity, string name, string description)
      {
         IFCAnyHandleUtil.SetAttribute(physicalQuantity, "Name", name);
         IFCAnyHandleUtil.SetAttribute(physicalQuantity, "Description", description);
      }

      /// <summary>
      /// Validates the values to be set to IfcPhysicalSimpleQuantity.
      /// </summary>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="unit">The unit.</param>
      private static void ValidatePhysicalSimpleQuantity(string name, string description, IFCAnyHandle unit)
      {
         ValidatePhysicalQuantity(name, description);
         IFCAnyHandleUtil.ValidateSubTypeOf(unit, true, IFCEntityType.IfcNamedUnit);
      }

      /// <summary>
      /// Sets attributes to IfcPhysicalSimpleQuantity.
      /// </summary>
      /// <param name="physicalSimpleQuantity">The IfcPhysicalSimpleQuantity.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="unit">The unit.</param>
      private static void SetPhysicalSimpleQuantity(IFCAnyHandle physicalSimpleQuantity, string name, string description, IFCAnyHandle unit)
      {
         IFCAnyHandleUtil.SetAttribute(physicalSimpleQuantity, "Unit", unit);
         SetPhysicalQuantity(physicalSimpleQuantity, name, description);
      }

      /// <summary>
      /// Validates the values to be set to IfcRepresentation.
      /// </summary>
      /// <param name="contextOfItems">The context of the items.</param>
      /// <param name="identifier">The identifier.</param>
      /// <param name="type">The representation type.</param>
      /// <param name="items">The items that belong to the shape representation.</param>
      private static void ValidateRepresentation(IFCAnyHandle contextOfItems, string identifier, string type, ICollection<IFCAnyHandle> items)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(contextOfItems, false, IFCEntityType.IfcRepresentationContext);
         IFCAnyHandleUtil.ValidateSubTypeOf(items, false, IFCEntityType.IfcRepresentationItem);
      }

      /// <summary>
      /// Sets attributes to IfcRepresentation.
      /// </summary>
      /// <param name="representation">The IfcRepresentation.</param>
      /// <param name="contextOfItems">The context of the items.</param>
      /// <param name="identifier">The identifier.</param>
      /// <param name="type">The representation type.</param>
      /// <param name="items">The items that belong to the shape representation.</param>
      private static void SetRepresentation(IFCAnyHandle representation, IFCAnyHandle contextOfItems, string identifier, string type, ISet<IFCAnyHandle> items)
      {
         IFCAnyHandleUtil.SetAttribute(representation, "ContextOfItems", contextOfItems);
         IFCAnyHandleUtil.SetAttribute(representation, "RepresentationIdentifier", identifier);
         IFCAnyHandleUtil.SetAttribute(representation, "RepresentationType", type);
         IFCAnyHandleUtil.SetAttribute(representation, "Items", items);
      }

      /// <summary>
      /// Validates the values to be set to IfcPresentationStyle.
      /// </summary>
      /// <param name="name">The name.</param>
      private static void ValidatePresentationStyle(string name)
      {
         //name can be optional
      }

      /// <summary>
      /// Sets attributes to IfcPresentationStyle.
      /// </summary>
      /// <param name="presentationStyle">The IfcPresentationStyle.</param>
      /// <param name="name">The name.</param>
      private static void SetPresentationStyle(IFCAnyHandle presentationStyle, string name)
      {
         IFCAnyHandleUtil.SetAttribute(presentationStyle, "Name", name);
      }

      /// <summary>
      /// Validates the values to be set to IfcPresentationLayerAssignment.
      /// </summary>
      /// <param name="name">The name.</param>
      private static void ValidatePresentationLayerAssignment(string name, ICollection<IFCAnyHandle> assignedItems)
      {
         if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException("name");

         IFCAnyHandleUtil.ValidateSubTypeOf(assignedItems, false, IFCEntityType.IfcRepresentationItem, IFCEntityType.IfcRepresentation);
         if (assignedItems.Count == 0)
            throw new ArgumentNullException("assignedItems");
      }

      private static void SetPresentationLayerAssigment(IFCAnyHandle presentationLayerAssigment, string name, string description, ISet<IFCAnyHandle> assignedItems, string identifier)
      {
         IFCAnyHandleUtil.SetAttribute(presentationLayerAssigment, "Name", name);
         IFCAnyHandleUtil.SetAttribute(presentationLayerAssigment, "Description", description);
         IFCAnyHandleUtil.SetAttribute(presentationLayerAssigment, "AssignedItems", assignedItems);
         IFCAnyHandleUtil.SetAttribute(presentationLayerAssigment, "Identifier", identifier);
      }

      /// <summary>
      /// Validates the values to be set to IfcPreDefinedItem.
      /// </summary>
      /// <param name="name">The name.</param>
      private static void ValidatePreDefinedItem(string name)
      {
         if (name == null)
            throw new ArgumentNullException("name");
      }

      /// <summary>
      /// Sets attributes to IfcPreDefinedItem.
      /// </summary>
      /// <param name="preDefinedItem">The IfcPreDefinedItem.</param>
      /// <param name="name">The name.</param>
      private static void SetPreDefinedItem(IFCAnyHandle preDefinedItem, string name)
      {
         IFCAnyHandleUtil.SetAttribute(preDefinedItem, "Name", name);
      }

      /// <summary>
      /// Sets attributes to IfcExternalReference.
      /// </summary>
      /// <param name="externalReference">The IfcExternalReference.</param>
      /// <param name="location">Location of the reference (e.g. URL).</param>
      /// <param name="itemReference">Location of the item within the reference source.</param>
      /// <param name="name">Name of the reference.</param>
      private static void SetExternalReference(IFCAnyHandle externalReference,
         string location, string itemReference, string name)
      {
         IFCAnyHandleUtil.SetAttribute(externalReference, "Location", location);
         IFCAnyHandleUtil.SetAttribute(externalReference, (ExporterCacheManager.ExportOptionsCache.ExportAs4) ? "Identification" : "ItemReference", itemReference);
         IFCAnyHandleUtil.SetAttribute(externalReference, "Name", name);
      }

      /// <summary>
      /// Validates the values to be set to IfcActor.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="theActor">The actor.</param>
      private static void ValidateActor(string guid, IFCAnyHandle ownerHistory, string name, string objectType, IFCAnyHandle theActor)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(theActor, false, IFCEntityType.IfcPerson, IFCEntityType.IfcPersonAndOrganization, IFCEntityType.IfcOrganization);
         ValidateObject(guid, ownerHistory);
      }

      /// <summary>
      /// Sets attributes to IfcActor.
      /// </summary>
      /// <param name="actor">The IfcActor.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="theActor">The actor.</param>
      private static void SetActor(IFCAnyHandle actor, string guid, IFCAnyHandle ownerHistory, string name, string description,
          string objectType, IFCAnyHandle theActor)
      {
         SetObject(actor, guid, ownerHistory, name, description, objectType);
         IFCAnyHandleUtil.SetAttribute(actor, "TheActor", theActor);
      }

      /// <summary>
      /// Validates the values to be set to IfcRelAssignsToActor.
      /// </summary>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">Related objects, which are assigned to a single object.</param>
      /// <param name="relatedObjectsType">Particular type of the assignment relationship.</param>
      /// <param name="relatingActor">The actor.</param>
      /// <param name="actingRole">The role of the actor.</param>
      private static void ValidateRelAssignsToActor(string guid, IFCAnyHandle ownerHistory,
          string name, string description, HashSet<IFCAnyHandle> relatedObjects, IFCObjectType? relatedObjectsType,
          IFCAnyHandle relatingActor, IFCAnyHandle actingRole)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(relatingActor, false, IFCEntityType.IfcActor);
         IFCAnyHandleUtil.ValidateSubTypeOf(actingRole, true, IFCEntityType.IfcActorRole);

         ValidateRelAssigns(guid, ownerHistory, name, relatedObjects, relatedObjectsType);
      }

      /// <summary>
      /// Sets attributes to IfcRelAssignsToActor.
      /// </summary>
      /// <param name="relActor">The IfcRelAssignsToActor.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">Related objects, which are assigned to a single object.</param>
      /// <param name="relatedObjectsType">Particular type of the assignment relationship.</param>
      /// <param name="relatingActor">The actor.</param>
      /// <param name="actingRole">The role of the actor.</param>
      private static void SetRelAssignsToActor(IFCAnyHandle relActor, string guid, IFCAnyHandle ownerHistory,
          string name, string description, HashSet<IFCAnyHandle> relatedObjects, IFCObjectType? relatedObjectsType,
          IFCAnyHandle relatingActor, IFCAnyHandle actingRole)
      {
         IFCAnyHandleUtil.SetAttribute(relActor, "RelatingActor", relatingActor);
         IFCAnyHandleUtil.SetAttribute(relActor, "ActingRole", actingRole);
         SetRelAssigns(relActor, guid, ownerHistory, name, description, relatedObjects, relatedObjectsType);
      }

      /// <summary>
      /// Sets attributes for IfcProfileDef.
      /// </summary>
      /// <param name="profileDef">The IfcProfileDef.</param>
      /// <param name="profileType">The profile type.</param>
      /// <param name="profileName">The profile name.</param>
      private static void SetProfileDef(IFCAnyHandle profileDef, IFCProfileType profileType, string profileName)
      {
         IFCAnyHandleUtil.SetAttribute(profileDef, "ProfileType", profileType);
         IFCAnyHandleUtil.SetAttribute(profileDef, "ProfileName", profileName);
      }

      /// <summary>
      /// Validates attributes for IfcParameterizedProfileDef.
      /// </summary>
      /// <param name="position">The profile position.</param>
      private static void ValidateParameterizedProfileDef(IFCAnyHandle position)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(position, false, IFCEntityType.IfcAxis2Placement2D);
      }

      /// <summary>
      /// Validates attributes for IfcCircleProfileDef.
      /// </summary>
      /// <param name="position">The profile position.</param>
      /// <param name="radius">The profile radius.</param>
      private static void ValidateCircleProfileDef(IFCAnyHandle position, double radius)
      {
         if (radius < MathUtil.Eps())
            throw new ArgumentException("Non-positive radius parameter.", "radius");

         ValidateParameterizedProfileDef(position);
      }

      /// <summary>
      /// Sets attributes for IfcParameterizedProfileDef.
      /// </summary>
      /// <param name="profileDef">The IfcProfileDef.</param>
      /// <param name="profileType">The profile type.</param>
      /// <param name="profileName">The profile name.</param>
      /// <param name="position">The profile position.</param>
      private static void SetParameterizedProfileDef(IFCAnyHandle profileDef, IFCProfileType profileType, string profileName, IFCAnyHandle position)
      {
         SetProfileDef(profileDef, profileType, profileName);
         IFCAnyHandleUtil.SetAttribute(profileDef, "Position", position);
      }

      /// <summary>
      /// Sets attributes for IfcCircleProfileDef.
      /// </summary>
      /// <param name="profileDef">The IfcCircleProfileDef.</param>
      /// <param name="profileType">The profile type.</param>
      /// <param name="profileName">The profile name.</param>
      /// <param name="position">The profile position.</param>
      private static void SetCircleProfileDef(IFCAnyHandle profileDef, IFCProfileType profileType, string profileName, IFCAnyHandle position, double radius)
      {
         SetParameterizedProfileDef(profileDef, profileType, profileName, position);
         IFCAnyHandleUtil.SetAttribute(profileDef, "Radius", radius);
      }

      /// <summary>
      /// Validates the attributes for IfcArbitraryOpenProfileDef or IfcArbitraryClosedProfileDef.
      /// </summary>
      /// <param name="outerCurve">The outer curve, of type IfcCurve and non-null.</param>
      private static void ValidateArbitraryOpenOrClosedProfileDef(IFCAnyHandle outerCurve)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(outerCurve, false, IFCEntityType.IfcCurve);
      }

      /// <summary>
      /// Sets attributes for IfcArbitraryClosedProfileDef.
      /// </summary>
      /// <param name="arbitraryClosedProfileDef">The IfcArbitraryClosedProfileDef.</param>
      /// <param name="profileType">The profile type.</param>
      /// <param name="profileName">The profile name.</param>
      /// <param name="outerCurve">The outer curve.</param>
      private static void SetArbitraryClosedProfileDef(IFCAnyHandle arbitraryClosedProfileDef, IFCProfileType profileType, string profileName,
          IFCAnyHandle outerCurve)
      {
         SetProfileDef(arbitraryClosedProfileDef, profileType, profileName);
         IFCAnyHandleUtil.SetAttribute(arbitraryClosedProfileDef, "OuterCurve", outerCurve);
      }

      /// <summary>
      /// Validates the attributes for IfcSweptAreaSolid.
      /// </summary>
      /// <param name="sweptArea">The profile.</param>
      /// <param name="sweptArea">The profile origin.</param>
      private static void ValidateSweptAreaSolid(IFCAnyHandle sweptArea, IFCAnyHandle position)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(sweptArea, false, IFCEntityType.IfcProfileDef);
         IFCAnyHandleUtil.ValidateSubTypeOf(position, false, IFCEntityType.IfcAxis2Placement3D);
      }

      /// <summary>
      /// Sets attributes for IfcSweptAreaSolid.
      /// </summary>
      /// <param name="SweptAreaSolid">The IfcSweptAreaSolid.</param>
      /// <param name="sweptArea">The profile.</param>
      /// <param name="position">The profile origin.</param>
      private static void SetSweptAreaSolid(IFCAnyHandle sweptAreaSolid, IFCAnyHandle sweptArea, IFCAnyHandle position)
      {
         IFCAnyHandleUtil.SetAttribute(sweptAreaSolid, "SweptArea", sweptArea);
         IFCAnyHandleUtil.SetAttribute(sweptAreaSolid, "Position", position);
      }

      /// <summary>
      /// Sets attributes for IfcSweptSurface.
      /// </summary>
      /// <param name="sweptSurface">The IfcSweptSurface.</param>
      /// <param name="sweptCurve">The curve.</param>
      /// <param name="position">The position.</param>
      private static void SetSweptSurface(IFCAnyHandle sweptSurface, IFCAnyHandle sweptCurve, IFCAnyHandle position)
      {
         IFCAnyHandleUtil.SetAttribute(sweptSurface, "SweptCurve", sweptCurve);
         IFCAnyHandleUtil.SetAttribute(sweptSurface, "Position", position);
      }

      /// <summary>
      /// Validates the attribute for IfcBSplineCurve
      /// </summary>
      /// <param name="controlPointsList">The list of control points</param>
      private static void ValidateBSplineCurve(IList<IFCAnyHandle> controlPointsList)
      {
         if (!ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            throw new InvalidOperationException("IfcBSplineCurve is only supported in IFC 4");
         }
         IFCAnyHandleUtil.ValidateSubTypeOf(controlPointsList, false, IFCEntityType.IfcCartesianPoint);
      }

      /// <summary>
      /// Set attributes for IfcBSplineCurveWithKnots
      /// </summary>
      /// <param name="bSplineCurveWithKnots">The IfcBSplineCurveWithKnots</param>
      /// <param name="degree">The degree</param>
      /// <param name="controlPointsList">The list of control points</param>
      /// <param name="curveForm">The curve form</param>
      /// <param name="closedCurve">Indicates whether this curve is closed or not (or unknown)</param>
      /// <param name="selfIntersect">Indicates whether this curve is self-intersect (or unknown)</param>
      /// <param name="knotMultiplicities">The knot multiplicites</param>
      /// <param name="knots">The list of disctinct knots, the multiplicity of each knot is stored in knotMultiplicities</param>
      /// <param name="knotSpec">The description of knot type</param>
      private static void SetBSplineCurveWithKnots(IFCAnyHandle bSplineCurveWithKnots, int degree, IList<IFCAnyHandle> controlPointsList, IFC4.IFCBSplineCurveForm curveForm,
          IFCLogical closedCurve, IFCLogical selfIntersect, IList<int> knotMultiplicities, IList<double> knots, IFC4.IFCKnotType knotSpec)
      {
         IFCAnyHandleUtil.SetAttribute(bSplineCurveWithKnots, "KnotMultiplicities", knotMultiplicities);
         IFCAnyHandleUtil.SetAttribute(bSplineCurveWithKnots, "Knots", knots);
         IFCAnyHandleUtil.SetAttribute(bSplineCurveWithKnots, "KnotSpec", knotSpec);
         SetBSplineCurve(bSplineCurveWithKnots, degree, controlPointsList, curveForm, closedCurve, selfIntersect);
      }

      /// <summary>
      /// Set attributes for IfcBSplineCurve
      /// </summary>
      /// <param name="bSplineCurve">The IfcBSplineCurve</param>
      /// <param name="degree">The degree</param>
      /// <param name="controlPointsList">The list of control points</param>
      /// <param name="curveForm">The curve form</param>
      /// <param name="closedCurve">Indicates whether this curve is closed or not, (or unknown)</param>
      /// <param name="selfIntersect">Indicates whether this curve is self-intersect or not, (or unknown)</param>
      private static void SetBSplineCurve(IFCAnyHandle bSplineCurve, int degree, IList<IFCAnyHandle> controlPointsList, IFC4.IFCBSplineCurveForm curveForm,
          IFCLogical closedCurve, IFCLogical selfIntersect)
      {
         ValidateBSplineCurve(controlPointsList);
         IFCAnyHandleUtil.SetAttribute(bSplineCurve, "Degree", degree);
         IFCAnyHandleUtil.SetAttribute(bSplineCurve, "ControlPointsList", controlPointsList);
         IFCAnyHandleUtil.SetAttribute(bSplineCurve, "CurveForm", curveForm);
         IFCAnyHandleUtil.SetAttribute(bSplineCurve, "ClosedCurve", closedCurve);
         IFCAnyHandleUtil.SetAttribute(bSplineCurve, "SelfIntersect", selfIntersect);
      }
      #endregion

      #region public creation methods goes here

      /// <summary>
      /// Creates a handle representing an IfcWall and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID to use to label the wall.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The representation object assigned to the wall.</param>
      /// <param name="elementTag">The tag for the identifier of the element.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateWall(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string preDefinedType)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle wall = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcWall, element);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            SetSpecificEnumAttr(wall, "PredefinedType", preDefinedType, "IfcWallType");

         SetElement(exporterIFC, wall, element, guid, ownerHistory, objectPlacement, representation);
         return wall;
      }

      /// <summary>
      /// Creates a handle representing an IfcWallStandardCase and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID to use to label the wall.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="elementTag">The tag for the identifier of the element.</param>
      /// <param name="representation">The representation object assigned to the wall.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateWallStandardCase(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string preDefinedType)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle wallStandardCase;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            wallStandardCase = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcWall, element);   // We export IfcWall only beginning IFC4
            SetSpecificEnumAttr(wallStandardCase, "PredefinedType", preDefinedType, "IfcWallType");
            //string validatedType = IFCValidateEntry.ValidateStrEnum<IFC4.IFCWallType>(preDefinedType);
            //// Special check here because .STANDARD. has been deprecated in IFC4 even though the definition still contains the Enum 
            //// for backward compatibility.
            //if (String.Compare(validatedType, "STANDARD", true) == 0)
            //   validatedType = "NOTDEFINED";
            //IFCAnyHandleUtil.SetAttribute(wallStandardCase, "PreDefinedType", validatedType, true);
         }
         else
            wallStandardCase = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcWallStandardCase, element);

         SetElement(exporterIFC, wallStandardCase, element, guid, ownerHistory, objectPlacement, representation);
         return wallStandardCase;
      }

      /// <summary>
      /// Creates a handle representing an IfcFootingType and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID to use to label the wall.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      /// <remarks>IfcFootingType is an IFC4 entity.  For previous versions, IfcTypeObject is used, which does not
      /// support representationMaps, elementTag, elementType, or predefinedType.</remarks>
      public static IFCAnyHandle CreateFootingType(IFCFile file, Element revitType, HashSet<IFCAnyHandle> propertySets, List<IFCAnyHandle> representationMaps, string predefinedType)
      {

         IFCAnyHandle footingType = null;

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            footingType = CreateInstance(file, IFCEntityType.IfcFootingType, revitType);
            SetSpecificEnumAttr(footingType, "PredefinedType", predefinedType, "IfcFootingType");
            //string validatedType = IFCValidateEntry.ValidateStrEnum<IFCFootingType>(predefinedType);
            //IFCAnyHandleUtil.SetAttribute(footingType, "PredefinedType", validatedType, true);
            SetElementType(footingType, revitType, propertySets, representationMaps);
         }
         else
         {
            // TODO: warn if representationMaps, elementTag, elementType, or predefinedType are non-null.
            footingType = CreateInstance(file, IFCEntityType.IfcTypeObject, revitType);
            SetTypeObject(footingType, revitType, propertySets);
         }

         return footingType;
      }

      /// <summary>
      /// Creates a handle representing an IfcCurtainWallType and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID to use to label the wall.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      /// <remarks>IfcCurtainWallType is new to IFC2x3; we will use IfcTypeObject for IFC2x2.</remarks>
      public static IFCAnyHandle CreateCurtainWallType(IFCFile file, Element revitType, HashSet<IFCAnyHandle> propertySets,
          List<IFCAnyHandle> representationMaps, string elementTag, string predefinedType)
      {
         IFCAnyHandle curtainWallType = null;
         if (ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
         {
            curtainWallType = CreateInstance(file, IFCEntityType.IfcTypeObject, revitType);
            SetTypeObject(curtainWallType, revitType, propertySets);
         }
         else
         {
            curtainWallType = CreateInstance(file, IFCEntityType.IfcCurtainWallType, revitType);
            SetSpecificEnumAttr(curtainWallType, "PredefinedType", predefinedType, "IfcCurtainWallType");
            
            SetElementType(curtainWallType, revitType, propertySets, representationMaps);
         }

         return curtainWallType;
      }

      /// <summary>
      /// Creates a handle representing an IfcWallType and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID to use to label the wall.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateWallType(IFCFile file, Element revitType, HashSet<IFCAnyHandle> propertySets,
          List<IFCAnyHandle> representationMaps, string predefinedType)
      {
         //string validatedType;

         IFCAnyHandle wallType = CreateInstance(file, IFCEntityType.IfcWallType, revitType);
         SetSpecificEnumAttr(wallType, "PredefinedType", predefinedType, "IfcWallType");

         SetElementType(wallType, revitType, propertySets, representationMaps);
         return wallType;
      }

      /// <summary>
      /// Creates a handle representing an IfcProductDefinitionShape and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="representations">The collection of representations assigned to the shape.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateProductDefinitionShape(IFCFile file, string name, string description, IList<IFCAnyHandle> representations)
      {
         ValidateProductRepresentation(name, description, representations);

         IFCAnyHandle productDefinitionShape = CreateInstance(file, IFCEntityType.IfcProductDefinitionShape, null);
         SetProductRepresentation(productDefinitionShape, name, description, representations);
         return productDefinitionShape;
      }

      /// <summary>
      /// Creates a handle representing an IfcBoundingBox and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="corner">The lower left corner of the bounding box.</param>
      /// <param name="xDim">The positive length in the X-direction.</param>
      /// <param name="yDim">The positive length in the Y-direction.</param>
      /// <param name="zDim">The positive length in the Z-direction.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateBoundingBox(IFCFile file, IFCAnyHandle corner, double xDim, double yDim, double zDim)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(corner, false, IFCEntityType.IfcCartesianPoint);

         if (xDim < MathUtil.Eps())
            throw new ArgumentOutOfRangeException("xDim", "The x-Value of the bounding box must be positive.");
         if (yDim < MathUtil.Eps())
            throw new ArgumentOutOfRangeException("yDim", "The y-Value of the bounding box must be positive.");
         if (zDim < MathUtil.Eps())
            throw new ArgumentOutOfRangeException("zDim", "The z-Value of the bounding box must be positive.");

         IFCAnyHandle boundingBox = CreateInstance(file, IFCEntityType.IfcBoundingBox, null);
         boundingBox.SetAttribute("Corner", corner);
         boundingBox.SetAttribute("XDim", xDim);
         boundingBox.SetAttribute("YDim", yDim);
         boundingBox.SetAttribute("ZDim", zDim);
         return boundingBox;
      }

      /// <summary>
      /// Creates a handle representing an IfcConnectedFaceSet and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="faces">The collection of faces.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateConnectedFaceSet(IFCFile file, HashSet<IFCAnyHandle> faces)
      {
         ValidateConnectedFaceSet(faces);

         IFCAnyHandle connectedFaceSet = CreateInstance(file, IFCEntityType.IfcConnectedFaceSet, null);
         SetConnectedFaceSet(connectedFaceSet, faces);
         return connectedFaceSet;
      }

      /// <summary>
      /// Creates a handle representing an IfcClosedShell and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="faces">The collection of faces.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateClosedShell(IFCFile file, HashSet<IFCAnyHandle> faces)
      {
         ValidateConnectedFaceSet(faces);

         IFCAnyHandle closedShell = CreateInstance(file, IFCEntityType.IfcClosedShell, null);
         SetConnectedFaceSet(closedShell, faces);
         return closedShell;
      }

      /// <summary>
      /// Creates a handle representing an IfcOpenShell and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="faces">The collection of faces.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateOpenShell(IFCFile file, HashSet<IFCAnyHandle> faces)
      {
         ValidateConnectedFaceSet(faces);

         IFCAnyHandle openShell = CreateInstance(file, IFCEntityType.IfcOpenShell, null);
         SetConnectedFaceSet(openShell, faces);
         return openShell;
      }

      /// <summary>
      /// Creates a handle representing an IfcFaceBasedSurfaceModel and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="faces">The collection of faces.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFaceBasedSurfaceModel(IFCFile file, HashSet<IFCAnyHandle> faces)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(faces, false, IFCEntityType.IfcConnectedFaceSet);

         IFCAnyHandle faceBasedSurfaceModel = CreateInstance(file, IFCEntityType.IfcFaceBasedSurfaceModel, null);
         IFCAnyHandleUtil.SetAttribute(faceBasedSurfaceModel, "FbsmFaces", faces);
         return faceBasedSurfaceModel;
      }

      /// <summary>
      /// Creates an IfcCovering, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID for the entity.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The geometric representation of the entity, in the IfcProductRepresentation.</param>
      /// <param name="elementTag">The tag for the identifier of the element.</param>
      /// <param name="coveringType">The covering type.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateCovering(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string coveringType)
      {
         string validatedType = coveringType;
         //coveringType can be optional
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle covering = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcCovering, element);
         SetSpecificEnumAttr(covering, "PredefinedType", coveringType, "IfcCoveringType");

         SetElement(exporterIFC, covering, element, guid, ownerHistory, objectPlacement, representation);
         return covering;
      }

      /// <summary>
      /// Creates an IfcFooting, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The geometric representation of the entity, in the IfcProductRepresentation.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="predefinedType">The footing type.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFooting(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string predefinedType)
      {
         string validatedType = predefinedType;

         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle footing = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcFooting, element);
         SetSpecificEnumAttr(footing, "PredefinedType", predefinedType, "IfcFootingType");

         SetElement(exporterIFC, footing, element, guid, ownerHistory, objectPlacement, representation);
         return footing;
      }

      /// <summary>
      /// Creates a handle representing an IfcSlab and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The geometric representation of the entity, in the IfcProductRepresentation.</param>
      /// <param name="representation"></param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="predefinedType">The slab type.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSlab(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string predefinedType)
      {
         //string validatedType;
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle slab = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcSlab, element);
         SetSpecificEnumAttr(slab, "PredefinedType", predefinedType, "IfcSlabType");

         SetElement(exporterIFC, slab, element, guid, ownerHistory, objectPlacement, representation);
         return slab;
      }

      /// <summary>
      /// Creates a handle representing an IfcCurtainWall and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID for the entity.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The geometric representation of the entity, in the IfcProductRepresentation.</param>
      /// <returns>The handle.</returns>
      /// <param name="elementTag">The tag for the identifier of the element.</param>
      public static IFCAnyHandle CreateCurtainWall(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string predefinedType)
      {
         //string validatedType;
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle curtainWall = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcCurtainWall, element);
         SetSpecificEnumAttr(curtainWall, "PredefinedType", predefinedType, "IfcCurtainWallType");

         SetElement(exporterIFC, curtainWall, element, guid, ownerHistory, objectPlacement, representation);
         return curtainWall;
      }

      /// <summary>
      /// Creates a handle representing an IfcPile and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID for the entity.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The geometric representation of the entity, in the IfcProductRepresentation.</param>
      /// <param name="elementTag">The tag for the identifier of the element.</param>
      /// <param name="preDefinedType">The pile type.</param>
      /// <param name="constructionType">The optional material for the construction of the pile.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePile(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string preDefinedType, IFCPileConstructionEnum? constructionType)
      {
         string validatedType = preDefinedType;

         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle pile = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcPile, element);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            SetSpecificEnumAttr(pile, "PredefinedType", preDefinedType, "IfcPileType");
            SetSpecificEnumAttr(pile, "ConstructionType", constructionType.ToString(), "IfcPileConstruction");
         }
         else
         {
            SetSpecificEnumAttr(pile, "PredefinedType", preDefinedType, "IfcPileType");
            SetSpecificEnumAttr(pile, "ConstructionType", constructionType.ToString(), "IFCPileConstructionEnum");
         }

         SetElement(exporterIFC, pile, element, guid, ownerHistory, objectPlacement, representation);
         return pile;
      }

      /// <summary>
      /// Creates a handle representing an IfcRailing and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID for the entity.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The geometric representation of the entity, in the IfcProductRepresentation.</param>
      /// <param name="elementTag">The tag for the identifier of the element.</param>
      /// <param name="predefinedType">The railing type.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRailing(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string predefinedType)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle railing = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcRailing, element);
         SetSpecificEnumAttr(railing, "PredefinedType", predefinedType, "IfcRailingType");

         SetElement(exporterIFC, railing, element, guid, ownerHistory, objectPlacement, representation);
         return railing;
      }

      /// <summary>
      /// Creates a handle representing an IfcRamp and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID for the entity.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The geometric representation of the entity, in the IfcProductRepresentation.</param>
      /// <param name="elementTag">The tag for the identifier of the element.</param>
      /// <param name="shapeType">The ramp type.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRamp(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string shapeType)
      {
         //string validatedType;

         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle ramp = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcRamp, element);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            SetSpecificEnumAttr(ramp, "PredefinedType", shapeType, "IfcRampType");
         }
         else
         {
            SetSpecificEnumAttr(ramp, "ShapeType", shapeType, "IfcRampType");
         }

         SetElement(exporterIFC, ramp, element, guid, ownerHistory, objectPlacement, representation);
         return ramp;
      }

      /// <summary>
      /// Creates a handle representing an IfcRoof and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID for the entity.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The geometric representation of the entity, in the IfcProductRepresentation.</param>
      /// <param name="elementTag">The tag for the identifier of the element.</param>
      /// <param name="shapeType">The roof type.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRoof(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string shapeType)
      {
         string validatedType = shapeType;
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle roof = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcRoof, element);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            SetSpecificEnumAttr(roof, "PredefinedType", shapeType, "IfcRoofType");
         }
         else
         {
            SetSpecificEnumAttr(roof, "ShapeType", shapeType, "IfcRoofType");
         }
         SetElement(exporterIFC, roof, element, guid, ownerHistory, objectPlacement, representation);
         return roof;
      }

      /// <summary>
      /// Creates a handle representing an IfcStair and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID for the entity.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The geometric representation of the entity, in the IfcProductRepresentation.</param>
      /// <param name="elementTag">The tag for the identifier of the element.</param>
      /// <param name="shapeType">The stair type.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateStair(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string shapeType)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle stair = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcStair, element);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            SetSpecificEnumAttr(stair, "PredefinedType", shapeType, "IfcStairType");
         }
         else
         {
            SetSpecificEnumAttr(stair, "ShapeType", shapeType, "IfcStairType");
         }
         SetElement(exporterIFC, stair, element, guid, ownerHistory, objectPlacement, representation);
         return stair;
      }

      /// <summary>
      /// Creates a handle representing an IfcStairFlight and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID for the entity.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The geometric representation of the entity, in the IfcProductRepresentation.</param>
      /// <param name="elementTag">The tag for the identifier of the element.</param>
      /// <param name="shapeType">The stair type.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateStairFlight(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation,
          int? numberOfRiser, int? numberOfTreads, double? riserHeight, double? treadLength, string preDefinedType)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle stairFlight = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcStairFlight, element);
         SetElement(exporterIFC, stairFlight, element, guid, ownerHistory, objectPlacement, representation);

         string numberOfRisersAttrName = (ExporterCacheManager.ExportOptionsCache.ExportAs4_ADD1 || ExporterCacheManager.ExportOptionsCache.ExportAs4_ADD2) ? "NumberOfRisers" : "NumberOfRiser";
         IFCAnyHandleUtil.SetAttribute(stairFlight, numberOfRisersAttrName, numberOfRiser);
         IFCAnyHandleUtil.SetAttribute(stairFlight, "NumberOfTreads", numberOfTreads);
         IFCAnyHandleUtil.SetAttribute(stairFlight, "RiserHeight", riserHeight);
         IFCAnyHandleUtil.SetAttribute(stairFlight, "TreadLength", treadLength);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            SetSpecificEnumAttr(stairFlight, "PredefinedType", preDefinedType, "IfcStairFlightType");
         }

         return stairFlight;
      }

      /// <summary>
      /// Creates a handle representing an IfcRampFlight and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID for the entity.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The geometric representation of the entity, in the IfcProductRepresentation.</param>
      /// <param name="elementTag">The tag for the identifier of the element.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRampFlight(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string preDefinedType)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle rampFlight = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcRampFlight, element);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            SetSpecificEnumAttr(rampFlight, "PredefinedType", preDefinedType, "IfcRampFlightType");
         }

         SetElement(exporterIFC, rampFlight, element, guid, ownerHistory, objectPlacement, representation);
         return rampFlight;
      }

      private static void SetReinforcingElement(ExporterIFC exporterIFC, IFCAnyHandle reinforcingElement, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string steelGrade)
      {
         SetElement(exporterIFC, reinforcingElement, element, guid, ownerHistory, objectPlacement, representation);

         //SteelGrade attribute has been deprecated in IFC4
         if (!ExporterCacheManager.ExportOptionsCache.ExportAs4)
            IFCAnyHandleUtil.SetAttribute(reinforcingElement, "SteelGrade", steelGrade);
      }

      private static void ValidSurfaceStyleShading(IFCAnyHandle surfaceColour)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(surfaceColour, true, IFCEntityType.IfcColourRgb);
      }

      private static void SetSurfaceStyleShading(IFCAnyHandle surfaceStyleRendering, IFCAnyHandle surfaceColour)
      {
         surfaceStyleRendering.SetAttribute("SurfaceColour", surfaceColour);
      }

      private static void ValidateBooleanResult(IFCAnyHandle firstOperand, IFCAnyHandle secondOperand)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(firstOperand, false, IFCEntityType.IfcSolidModel, IFCEntityType.IfcBooleanResult,
             IFCEntityType.IfcHalfSpaceSolid, IFCEntityType.IfcCsgPrimitive3D);
         IFCAnyHandleUtil.ValidateSubTypeOf(secondOperand, false, IFCEntityType.IfcHalfSpaceSolid, IFCEntityType.IfcSolidModel,
             IFCEntityType.IfcBooleanResult, IFCEntityType.IfcCsgPrimitive3D);
      }

      private static void SetBooleanResult(IFCAnyHandle booleanResultHnd, IFCBooleanOperator clipOperator,
          IFCAnyHandle firstOperand, IFCAnyHandle secondOperand)
      {
         IFCAnyHandleUtil.SetAttribute(booleanResultHnd, "Operator", clipOperator);
         IFCAnyHandleUtil.SetAttribute(booleanResultHnd, "FirstOperand", firstOperand);
         IFCAnyHandleUtil.SetAttribute(booleanResultHnd, "SecondOperand", secondOperand);
      }

      private static void ValidateElementarySurface(IFCAnyHandle position)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(position, false, IFCEntityType.IfcAxis2Placement3D);
      }

      private static void SetElementarySurface(IFCAnyHandle elementarySurfaceHnd, IFCAnyHandle position)
      {
         IFCAnyHandleUtil.SetAttribute(elementarySurfaceHnd, "Position", position);
      }

      private static void ValidateHalfSpaceSolid(IFCAnyHandle baseSurface)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(baseSurface, false, IFCEntityType.IfcSurface);
      }

      private static void SetHalfSpaceSolid(IFCAnyHandle halfSpaceSolidHnd, IFCAnyHandle baseSurface, bool agreementFlag)
      {
         IFCAnyHandleUtil.SetAttribute(halfSpaceSolidHnd, "BaseSurface", baseSurface);
         IFCAnyHandleUtil.SetAttribute(halfSpaceSolidHnd, "AgreementFlag", agreementFlag);
      }

      private static void ValidateConic(IFCAnyHandle position)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(position, false, IFCEntityType.IfcAxis2Placement2D, IFCEntityType.IfcAxis2Placement3D);
      }

      private static void SetConic(IFCAnyHandle conic, IFCAnyHandle position)
      {
         IFCAnyHandleUtil.SetAttribute(conic, "Position", position);
      }

      /// <summary>
      /// Creates a handle representing an IfcReinforcingBar and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID for the entity.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The geometric representation of the entity, in the IfcProductRepresentation.</param>
      /// <param name="elementTag">The tag for the identifier of the element.</param>
      /// <param name="steelGrade">The steel grade.</param>
      /// <param name="longitudinalBarNominalDiameter">The nominal diameter.</param>
      /// <param name="longitudinalBarCrossSectionArea">The cross section area.</param>
      /// <param name="barLength">The bar length (optional).</param>
      /// <param name="role">The role.</param>
      /// <param name="surface">The surface (optional).</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateReinforcingBar(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string steelGrade,
          double longitudinalBarNominalDiameter, double longitudinalBarCrossSectionArea,
          double? barLength, IFCReinforcingBarRole role, IFCReinforcingBarSurface? surface)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         string predefinedTypeAttribName = ExporterCacheManager.ExportOptionsCache.ExportAs4 ? "PredefinedType" : "BarRole";

         IFCAnyHandle reinforcingBar = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcReinforcingBar, element);
         SetReinforcingElement(exporterIFC, reinforcingBar, element, guid, ownerHistory, objectPlacement,
             representation, steelGrade);
         IFCAnyHandleUtil.SetAttribute(reinforcingBar, "NominalDiameter", longitudinalBarNominalDiameter);
         IFCAnyHandleUtil.SetAttribute(reinforcingBar, "CrossSectionArea", longitudinalBarCrossSectionArea);
         if (barLength != null)
            IFCAnyHandleUtil.SetAttribute(reinforcingBar, "BarLength", barLength);
         IFCAnyHandleUtil.SetAttribute(reinforcingBar, predefinedTypeAttribName, role);
         if (surface != null)
            IFCAnyHandleUtil.SetAttribute(reinforcingBar, "BarSurface", surface);

         return reinforcingBar;
      }

      /// <summary>
      /// Creates a handle representing an IfcReinforcingBar and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID for the entity.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The geometric representation of the entity, in the IfcProductRepresentation.</param>
      /// <param name="elementTag">The tag for the identifier of the element.</param>
      /// <param name="steelGrade">The steel grade.</param>
      /// <param name="longitudinalBarNominalDiameter">The nominal diameter.</param>
      /// <param name="longitudinalBarCrossSectionArea">The cross section area.</param>
      /// <param name="barLength">The bar length (optional).</param>
      /// <param name="role">The role.</param>
      /// <param name="surface">The surface (optional).</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateReinforcingMesh(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string steelGrade,
          double? meshLength, double? meshWidth,
          double longitudinalBarNominalDiameter, double transverseBarNominalDiameter,
          double longitudinalBarCrossSectionArea, double transverseBarCrossSectionArea,
          double longitudinalBarSpacing, double transverseBarSpacing)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle reinforcingMesh = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcReinforcingMesh, element);
         SetReinforcingElement(exporterIFC, reinforcingMesh, element, guid, ownerHistory, objectPlacement, representation, steelGrade);
         if (meshLength != null)
            IFCAnyHandleUtil.SetAttribute(reinforcingMesh, "MeshLength", meshLength);
         if (meshWidth != null)
            IFCAnyHandleUtil.SetAttribute(reinforcingMesh, "MeshWidth", meshWidth);

         IFCAnyHandleUtil.SetAttribute(reinforcingMesh, "LongitudinalBarNominalDiameter", longitudinalBarNominalDiameter);
         IFCAnyHandleUtil.SetAttribute(reinforcingMesh, "TransverseBarNominalDiameter", transverseBarNominalDiameter);
         IFCAnyHandleUtil.SetAttribute(reinforcingMesh, "LongitudinalBarCrossSectionArea", longitudinalBarCrossSectionArea);
         IFCAnyHandleUtil.SetAttribute(reinforcingMesh, "TransverseBarCrossSectionArea", transverseBarCrossSectionArea);
         IFCAnyHandleUtil.SetAttribute(reinforcingMesh, "LongitudinalBarSpacing", longitudinalBarSpacing);
         IFCAnyHandleUtil.SetAttribute(reinforcingMesh, "TransverseBarSpacing", transverseBarSpacing);

         return reinforcingMesh;
      }

      /// <summary>
      /// Creates an IfcRelContainedInSpatialStructure, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID for the entity.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatingObject">The element to which the structure contributes.</param>
      /// <param name="relatedObjects">The elements that make up the structure.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRelAggregates(IFCFile file, string guid, IFCAnyHandle ownerHistory,
          string name, string description, IFCAnyHandle relatingObject, HashSet<IFCAnyHandle> relatedObjects)
      {
         ValidateRelDecomposes(guid, ownerHistory, name, description, relatingObject, relatedObjects);

         IFCAnyHandle relAggregates = CreateInstance(file, IFCEntityType.IfcRelAggregates, null);
         SetRelDecomposes(relAggregates, guid, ownerHistory, name, description, relatingObject, relatedObjects);
         return relAggregates;
      }

      /// <summary>
      /// Creates an IfcLocalPlacement, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="placementRelTo">The parent placement.</param>
      /// <param name="relativePlacement">The local offset to the parent placement.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateLocalPlacement(IFCFile file, IFCAnyHandle placementRelTo, IFCAnyHandle relativePlacement)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(placementRelTo, true, IFCEntityType.IfcObjectPlacement);

         IFCAnyHandleUtil.ValidateSubTypeOf(relativePlacement, false, IFCEntityType.IfcAxis2Placement2D, IFCEntityType.IfcAxis2Placement3D);

         IFCAnyHandle localPlacement = CreateInstance(file, IFCEntityType.IfcLocalPlacement, null);
         IFCAnyHandleUtil.SetAttribute(localPlacement, "PlacementRelTo", placementRelTo);
         IFCAnyHandleUtil.SetAttribute(localPlacement, "RelativePlacement", relativePlacement);
         return localPlacement;
      }

      /// <summary>
      /// Creates an IfcProject, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="longName">The long name.</param>
      /// <param name="phase">Current project phase, open to interpretation for all project partner.</param>
      /// <param name="representationContexts">Context of the representations used within the project.</param>
      /// <param name="units">Units globally assigned to measure types used within the context of this project.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateProject(ExporterIFC exporterIFC, ProjectInfo projectInfo, string guid, IFCAnyHandle ownerHistory,
          string name, string description, string longName, string phase,
          HashSet<IFCAnyHandle> representationContexts, IFCAnyHandle units)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(representationContexts, false, IFCEntityType.IfcRepresentationContext);

         IFCAnyHandleUtil.ValidateSubTypeOf(units, false, IFCEntityType.IfcUnitAssignment);

         ValidateObject(guid, ownerHistory);

         IFCAnyHandle project = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcProject, null);
         IFCAnyHandleUtil.SetAttribute(project, "LongName", longName);
         IFCAnyHandleUtil.SetAttribute(project, "Phase", phase);
         IFCAnyHandleUtil.SetAttribute(project, "RepresentationContexts", representationContexts);
         IFCAnyHandleUtil.SetAttribute(project, "UnitsInContext", units);
         SetObject(exporterIFC, project, projectInfo, guid, ownerHistory);
         setRootName(project, name);
         setRootDescription(project, description);
         return project;
      }

      /// <summary>
      /// Creates an IfcBuilding, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The representation object.</param>
      /// <param name="longName">The long name.</param>
      /// <param name="compositionType">The composition type.</param>
      /// <param name="elevationOfRefHeight">Elevation above sea level of the reference height used for all storey elevation measures, equals to height 0.0.</param>
      /// <param name="elevationOfTerrain">Elevation above the minimal terrain level around the foot print of the building, given in elevation above sea level.</param>
      /// <param name="buildingAddress">Address given to the building for postal purposes.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateBuilding(ExporterIFC exporterIFC, string guid, IFCAnyHandle ownerHistory,
          string name, string description, IFCAnyHandle objectPlacement, IFCAnyHandle representation,
          string longName, IFCElementComposition compositionType, double? elevationOfRefHeight, double? elevationOfTerrain, IFCAnyHandle buildingAddress)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(buildingAddress, true, IFCEntityType.IfcPostalAddress);
         ValidateSpatialStructureElement(guid, ownerHistory, objectPlacement, representation, compositionType);

         IFCAnyHandle building = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcBuilding, null);
         IFCAnyHandleUtil.SetAttribute(building, "ElevationOfRefHeight", elevationOfRefHeight);
         IFCAnyHandleUtil.SetAttribute(building, "ElevationOfTerrain", elevationOfTerrain);
         IFCAnyHandleUtil.SetAttribute(building, "BuildingAddress", buildingAddress);
         SetSpatialStructureElement(exporterIFC, building, null, guid, ownerHistory, name, description, objectPlacement, representation, longName, compositionType);
         return building;
      }

      /// <summary>
      /// Creates a handle representing an IfcBuildingStorey and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The representation object.</param>
      /// <param name="longName">The long name.</param>
      /// <param name="compositionType">The composition type.</param>
      /// <param name="elevation">The elevation with flooring measurement.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateBuildingStorey(ExporterIFC exporterIFC, Level level, IFCAnyHandle ownerHistory, IFCAnyHandle objectPlacement,
          IFCElementComposition compositionType, double elevation)
      {


         IFCAnyHandle buildingStorey = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcBuildingStorey, null);
         string guid = GUIDUtil.GetLevelGUID(level);
         string name = NamingUtil.GetNameOverride(buildingStorey, level, level.Name);
         string description = NamingUtil.GetDescriptionOverride(level, null);
         string longName = NamingUtil.GetLongNameOverride(level, level.Name);
         ValidateSpatialStructureElement(guid, ownerHistory, objectPlacement, null, compositionType);

         IFCAnyHandleUtil.SetAttribute(buildingStorey, "Elevation", elevation);
         SetSpatialStructureElement(exporterIFC, buildingStorey, level, guid, ownerHistory, name, description, objectPlacement, null, longName, compositionType);
         return buildingStorey;
      }

      /// <summary>
      /// Creates a handle representing an IfcSpace and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The representation object.</param>
      /// <param name="longName">The long name.</param>
      /// <param name="compositionType">The composition type.</param>
      /// <param name="internalOrExternal">Specify if it is an exterior space (i.e. part of the outer space) or an interior space.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSpace(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation,
          IFCElementComposition compositionType, IFCInternalOrExternal internalOrExternal)
      {
         ValidateSpatialStructureElement(guid, ownerHistory, objectPlacement, representation, compositionType);
         string strSpaceNumber = null;
         string strSpaceName = null;
         string strSpaceDesc = null;

         if (ParameterUtil.GetStringValueFromElement(element, BuiltInParameter.ROOM_NUMBER, out strSpaceNumber) == null)
            strSpaceNumber = null;

         if (ParameterUtil.GetStringValueFromElement(element, BuiltInParameter.ROOM_NAME, out strSpaceName) == null)
            strSpaceName = null;

         if (ParameterUtil.GetStringValueFromElement(element, BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS, out strSpaceDesc) == null)
            strSpaceDesc = null;

         IFCAnyHandle space = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcSpace, element);
         string name = NamingUtil.GetNameOverride(space, element, strSpaceNumber);
         string desc = NamingUtil.GetDescriptionOverride(space, element, strSpaceDesc);
         string longName = NamingUtil.GetLongNameOverride(space, element, strSpaceName);
         double? spaceElevationWithFlooring = null;
         double elevationWithFlooring = 0.0;
         if (ParameterUtil.GetDoubleValueFromElement(element, null, "IfcElevationWithFlooring", out elevationWithFlooring) != null)
            spaceElevationWithFlooring = UnitUtil.ScaleLength(elevationWithFlooring);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            // In IFC4 the position of the attribute is replaced with PreDefinedType
            IFCAnyHandleUtil.SetAttribute(space, "PreDefinedType", IFC4.IFCSpaceType.SPACE);
         }
         else
         {
            // set this attribute only when it is exported to format PRIOR to IFC4. The attribute has been removed/replaced in IFC4 and the property is moved to property set Pset_SpaceCommon.IsExternal
            IFCAnyHandleUtil.SetAttribute(space, "InteriorOrExteriorSpace", internalOrExternal);
         }
         IFCAnyHandleUtil.SetAttribute(space, "ElevationWithFlooring", spaceElevationWithFlooring);
         SetSpatialStructureElement(exporterIFC, space, element, guid, ownerHistory, name, desc, objectPlacement, representation, longName, compositionType);

         return space;
      }

      /// <summary>
      /// Creates an IfcSpaceType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSpaceType(IFCFile file, Element revitType, HashSet<IFCAnyHandle> propertySets,
          IList<IFCAnyHandle> representationMaps)
      {
         IFCAnyHandle spaceType = CreateInstance(file, IFCEntityType.IfcSpaceType, revitType);
         SetElementType(spaceType, revitType, propertySets, representationMaps);
         return spaceType;
      }

      /// <summary>
      /// Creates an IfcRelCoversBldgElements, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatingBuildingElement">The element that is covered by one or more coverings.</param>
      /// <param name="relatedCoverings">The IfcCoverings covering the building element.</param>
      /// <returns>The handle.</returns>
      /// <remarks>This has been deprecated in IFC4, and will redirect to CreateRelAggregates instead.</remarks>
      public static IFCAnyHandle CreateRelCoversBldgElements(IFCFile file, string guid, IFCAnyHandle ownerHistory,
          string name, string description, IFCAnyHandle relatingBuildingElement, HashSet<IFCAnyHandle> relatedCoverings)
      {
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            return CreateRelAggregates(file, guid, ownerHistory, name, description, relatingBuildingElement, relatedCoverings);

         IFCAnyHandleUtil.ValidateSubTypeOf(relatingBuildingElement, false, IFCEntityType.IfcElement);
         IFCAnyHandleUtil.ValidateSubTypeOf(relatedCoverings, false, IFCEntityType.IfcCovering);
         ValidateRelConnects(guid, ownerHistory, name);

         IFCAnyHandle relCoversBldgElements = CreateInstance(file, IFCEntityType.IfcRelCoversBldgElements, null);
         IFCAnyHandleUtil.SetAttribute(relCoversBldgElements, "RelatingBuildingElement", relatingBuildingElement);
         IFCAnyHandleUtil.SetAttribute(relCoversBldgElements, "RelatedCoverings", relatedCoverings);
         SetRelConnects(relCoversBldgElements, guid, ownerHistory, name, description);
         return relCoversBldgElements;
      }

      /// <summary>
      /// Creates an IfcRelContainedInSpatialStructure, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedElements">The elements that make up the structure.</param>
      /// <param name="relateingElement">The element to which the structure contributes.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRelContainedInSpatialStructure(IFCFile file, string guid, IFCAnyHandle ownerHistory,
          string name, string description, HashSet<IFCAnyHandle> relatedElements, IFCAnyHandle relateingElement)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(relatedElements, false, IFCEntityType.IfcProduct);
         IFCAnyHandleUtil.ValidateSubTypeOf(relateingElement, false, IFCEntityType.IfcSpatialStructureElement);
         ValidateRelConnects(guid, ownerHistory, name);

         IFCAnyHandle relContainedInSpatialStructure = CreateInstance(file, IFCEntityType.IfcRelContainedInSpatialStructure, null);
         IFCAnyHandleUtil.SetAttribute(relContainedInSpatialStructure, "RelatedElements", relatedElements);
         IFCAnyHandleUtil.SetAttribute(relContainedInSpatialStructure, "RelatingStructure", relateingElement);
         SetRelConnects(relContainedInSpatialStructure, guid, ownerHistory, name, description);
         return relContainedInSpatialStructure;
      }

      /// <summary>
      /// Creates a handle representing a relationship (IfcRelAssociatesMaterial) between a material definition and elements 
      /// or element types to which this material definition applies.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">The objects to be related to the material.</param>
      /// <param name="relatingMaterial">The material.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRelAssociatesMaterial(IFCFile file, string guid, IFCAnyHandle ownerHistory,
          string name, string description, HashSet<IFCAnyHandle> relatedObjects, IFCAnyHandle relatingMaterial)
      {
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            IFCAnyHandleUtil.ValidateSubTypeOf(relatingMaterial, false, IFCEntityType.IfcMaterialDefinition, IFCEntityType.IfcMaterialList, IFCEntityType.IfcMaterialUsageDefinition);
         else
            IFCAnyHandleUtil.ValidateSubTypeOf(relatingMaterial, false, IFCEntityType.IfcMaterial, IFCEntityType.IfcMaterialList, IFCEntityType.IfcMaterialLayerSetUsage
              , IFCEntityType.IfcMaterialLayerSet);

         ValidateRelAssociates(guid, ownerHistory, name, description, relatedObjects);

         IFCAnyHandle relAssociatesMaterial = CreateInstance(file, IFCEntityType.IfcRelAssociatesMaterial, null);
         IFCAnyHandleUtil.SetAttribute(relAssociatesMaterial, "RelatingMaterial", relatingMaterial);
         SetRelAssociates(relAssociatesMaterial, guid, ownerHistory, name, description, relatedObjects);
         return relAssociatesMaterial;
      }

      /// <summary>
      /// Creates an IfcRelDefinesByType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">The objects to be related to a type.</param>
      /// <param name="relatingType">The relating type.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRelDefinesByType(IFCFile file, string guid, IFCAnyHandle ownerHistory,
          string name, string description, HashSet<IFCAnyHandle> relatedObjects, IFCAnyHandle relatingType)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(relatingType, false, IFCEntityType.IfcTypeObject);
         ValidateRelDefines(guid, ownerHistory, name, description, relatedObjects);

         IFCAnyHandle relDefinesByType = CreateInstance(file, IFCEntityType.IfcRelDefinesByType, null);
         IFCAnyHandleUtil.SetAttribute(relDefinesByType, "RelatingType", relatingType);
         SetRelDefines(relDefinesByType, guid, ownerHistory, name, description, relatedObjects);
         return relDefinesByType;
      }

      /// <summary>
      /// Creates a handle representing an IfcRelConnectsPathElements and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="connectionGeometry">The geometric shape representation of the connection geometry.</param>
      /// <param name="relatingElement">Reference to a subtype of IfcElement that is connected by the connection relationship in the role of RelatingElement.</param>
      /// <param name="relatedElement">Reference to a subtype of IfcElement that is connected by the connection relationship in the role of RelatedElement.</param>
      /// <param name="relatingPriorities">Priorities for connection.</param>
      /// <param name="relatedPriorities">Priorities for connection.</param>
      /// <param name="relatedConnectionType">The connection type in relation to the path of the RelatingObject.</param>
      /// <param name="relatingConnectionType">The connection type in relation to the path of the RelatingObject.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRelConnectsPathElements(IFCFile file, string guid, IFCAnyHandle ownerHistory,
          string name, string description, IFCAnyHandle connectionGeometry, IFCAnyHandle relatingElement, IFCAnyHandle relatedElement,
          IList<int> relatingPriorities, IList<int> relatedPriorities, IFCConnectionType relatedConnectionType, IFCConnectionType relatingConnectionType)
      {
         if (relatingPriorities == null)
            throw new ArgumentNullException("relatingPriorities");
         if (relatedPriorities == null)
            throw new ArgumentNullException("relatedPriorities");

         ValidateRelConnectsElements(guid, ownerHistory, name, connectionGeometry, relatingElement, relatedElement);

         IFCAnyHandle relConnectsPathElements = CreateInstance(file, IFCEntityType.IfcRelConnectsPathElements, null);
         IFCAnyHandleUtil.SetAttribute(relConnectsPathElements, "RelatingPriorities", relatingPriorities);
         IFCAnyHandleUtil.SetAttribute(relConnectsPathElements, "RelatedPriorities", relatedPriorities);
         IFCAnyHandleUtil.SetAttribute(relConnectsPathElements, "RelatedConnectionType", relatedConnectionType);
         IFCAnyHandleUtil.SetAttribute(relConnectsPathElements, "RelatingConnectionType", relatingConnectionType);
         SetRelConnectsElements(relConnectsPathElements, guid, ownerHistory, name, description, connectionGeometry, relatingElement, relatedElement);
         return relConnectsPathElements;
      }

      /// <summary>
      /// Creates a handle representing an IfcZone and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="longName">The long name, valid for IFC4+ schemas.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateZone(IFCFile file, string guid, IFCAnyHandle ownerHistory,
          string name, string description, string objectType, string longName)
      {
         ValidateGroup(guid, ownerHistory, name, objectType);

         IFCAnyHandle zone = CreateInstance(file, IFCEntityType.IfcZone, null);
         SetGroup(zone, guid, ownerHistory, name, description, objectType);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            IFCAnyHandleUtil.SetAttribute(zone, "LongName", longName);

         return zone;
      }

      /// <summary>
      /// Creates a handle representing an IfcOccupant and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="theActor">The actor.</param>
      /// <param name="predefinedType">Predefined occupant types.</param>
      /// <returns></returns>
      public static IFCAnyHandle CreateOccupant(IFCFile file, string guid, IFCAnyHandle ownerHistory, string name, string description,
          string objectType, IFCAnyHandle theActor, IFCOccupantType predefinedType)
      {
         ValidateActor(guid, ownerHistory, name, objectType, theActor);

         IFCAnyHandle occupant = CreateInstance(file, IFCEntityType.IfcOccupant, null);
         SetActor(occupant, guid, ownerHistory, name, description, objectType, theActor);
         IFCAnyHandleUtil.SetAttribute(occupant, "PredefinedType", predefinedType);

         return occupant;
      }

      /// <summary>
      /// Creates a handle representing an IfcRelAssignsToGroup and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">Related objects, which are assigned to a single object.</param>
      /// <param name="relatedObjectsType">Particular type of the assignment relationship.</param>
      /// <param name="relatingGroup">Reference to group that finally contains all assigned group members.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRelAssignsToGroup(IFCFile file, string guid, IFCAnyHandle ownerHistory,
          string name, string description, ISet<IFCAnyHandle> relatedObjects, IFCObjectType? relatedObjectsType, IFCAnyHandle relatingGroup)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(relatingGroup, false, IFCEntityType.IfcGroup);

         ValidateRelAssigns(guid, ownerHistory, name, relatedObjects, relatedObjectsType);

         IFCAnyHandle relAssignsToGroup = CreateInstance(file, IFCEntityType.IfcRelAssignsToGroup, null);
         IFCAnyHandleUtil.SetAttribute(relAssignsToGroup, "RelatingGroup", relatingGroup);
         SetRelAssigns(relAssignsToGroup, guid, ownerHistory, name, description, relatedObjects, relatedObjectsType);
         return relAssignsToGroup;
      }

      /// <summary>
      /// Creates a handle representing an IfcRelAssignsToActor and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">Related objects, which are assigned to a single object.</param>
      /// <param name="relatedObjectsType">Particular type of the assignment relationship.</param>
      /// <param name="relatingActor">Reference to the information about the actor.</param>
      /// <param name="actingRole">Role of the actor played within the context of the assignment to the object(s).</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRelAssignsToActor(IFCFile file, string guid, IFCAnyHandle ownerHistory,
          string name, string description, ISet<IFCAnyHandle> relatedObjects, IFCObjectType? relatedObjectsType, IFCAnyHandle relatingActor, IFCAnyHandle actingRole)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(relatingActor, false, IFCEntityType.IfcActor);
         IFCAnyHandleUtil.ValidateSubTypeOf(actingRole, true, IFCEntityType.IfcActorRole);

         ValidateRelAssigns(guid, ownerHistory, name, relatedObjects, relatedObjectsType);

         IFCAnyHandle relAssignsToActor = CreateInstance(file, IFCEntityType.IfcRelAssignsToActor, null);
         IFCAnyHandleUtil.SetAttribute(relAssignsToActor, "RelatingActor", relatingActor);
         IFCAnyHandleUtil.SetAttribute(relAssignsToActor, "ActingRole", actingRole);
         SetRelAssigns(relAssignsToActor, guid, ownerHistory, name, description, relatedObjects, relatedObjectsType);
         return relAssignsToActor;
      }

      /// <summary>
      /// Creates a handle representing an IfcRelOccupiesSpaces and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">Related objects, which are assigned to a single object.</param>
      /// <param name="relatedObjectsType">Particular type of the assignment relationship.</param>
      /// <param name="relatingActor">The actor.</param>
      /// <param name="actingRole">The role of the actor.</param>
      /// <returns>The handle.</returns>
      /// <remarks>Note that this has been obsoleted in IFC4, and replaced by IfcRelAssignsToActor.</remarks>
      public static IFCAnyHandle CreateRelOccupiesSpaces(IFCFile file, string guid, IFCAnyHandle ownerHistory,
          string name, string description, HashSet<IFCAnyHandle> relatedObjects, IFCObjectType? relatedObjectsType,
          IFCAnyHandle relatingActor, IFCAnyHandle actingRole)
      {
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            return CreateRelAssignsToActor(file, guid, ownerHistory, name, description,
                relatedObjects, relatedObjectsType, relatingActor, actingRole);

         ValidateRelAssignsToActor(guid, ownerHistory, name, description, relatedObjects, relatedObjectsType,
             relatingActor, actingRole);

         IFCAnyHandle relOccupiesSpaces = CreateInstance(file, IFCEntityType.IfcRelOccupiesSpaces, null);
         SetRelAssignsToActor(relOccupiesSpaces, guid, ownerHistory, name, description, relatedObjects, relatedObjectsType,
             relatingActor, actingRole);
         return relOccupiesSpaces;
      }

      /// <summary>
      /// Creates a handle representing an IfcPropertySet and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="hasProperties">The collection of properties.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePropertySet(IFCFile file, string guid, IFCAnyHandle ownerHistory,
          string name, string description, ISet<IFCAnyHandle> hasProperties)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(hasProperties, false, IFCEntityType.IfcProperty);
         ValidatePropertySetDefinition(guid, ownerHistory);

         IFCAnyHandle propertySet = CreateInstance(file, IFCEntityType.IfcPropertySet, null);
         IFCAnyHandleUtil.SetAttribute(propertySet, "HasProperties", hasProperties);
         SetPropertySetDefinition(propertySet, guid, ownerHistory, name, description);
         return propertySet;
      }

      /// <summary>
      /// Creates a handle representing a IfcRelDefinesByProperties and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">Related objects, which are assigned to a single object.</param>
      /// <param name="relatingPropertyDefinition">The relating proprety definition.</param>
      /// <returns>The handle.</returns>
      /// <remarks>In IFC4, we are only allowed one relatedObject.</remarks>
      public static IFCAnyHandle CreateRelDefinesByProperties(IFCFile file, string guid, IFCAnyHandle ownerHistory,
          string name, string description, IFCAnyHandle relatedObject, IFCAnyHandle relatingPropertyDefinition)
      {
         // We expect ValidateRelDefines to validate relatedObject.
         ISet<IFCAnyHandle> relatedObjects = new HashSet<IFCAnyHandle>();
         relatedObjects.Add(relatedObject);

         IFCAnyHandleUtil.ValidateSubTypeOf(relatingPropertyDefinition, false, IFCEntityType.IfcPropertySetDefinition);
         ValidateRelDefines(guid, ownerHistory, name, description, relatedObjects);

         return CreateRelDefinesByProperties(file, guid, ownerHistory, name, description, relatedObjects, relatingPropertyDefinition);
      }

      /// <summary>
      /// Creates a handle representing a IfcRelDefinesByProperties and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatedObjects">Related objects, which are assigned to a single object.</param>
      /// <param name="relatingPropertyDefinition">The relating proprety definition.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRelDefinesByProperties(IFCFile file, string guid, IFCAnyHandle ownerHistory,
          string name, string description, ISet<IFCAnyHandle> relatedObjects, IFCAnyHandle relatingPropertyDefinition)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(relatingPropertyDefinition, false, IFCEntityType.IfcPropertySetDefinition);
         ValidateRelDefines(guid, ownerHistory, name, description, relatedObjects);

         // ValidateRelDefines actually does more than usual - it checks relatedObjects for various sub-types.
         // For IFC4 we need to also check that there is only 1 object in relatedObjects.

         // We would like to do the check below, but because of the issue stated in ExportUtil.CreateRelDefinesByProperties,
         // we can't do it yet.  This will be turned on in a future release.
         //ValidateRelDefinesByPropertiesIFC4(relatedObjects);

         IFCAnyHandle relDefinesByProperties = CreateInstance(file, IFCEntityType.IfcRelDefinesByProperties, null);
         IFCAnyHandleUtil.SetAttribute(relDefinesByProperties, "RelatingPropertyDefinition", relatingPropertyDefinition);
         SetRelDefines(relDefinesByProperties, guid, ownerHistory, name, description, relatedObjects);
         return relDefinesByProperties;
      }

      /// <summary>
      /// Creates a handle representing an IfcComplexProperty and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="usageName">The name of the property.</param>
      /// <param name="hasProperties">The collection of the component properties.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateComplexProperty(IFCFile file, string name, string description, string usageName,
          HashSet<IFCAnyHandle> hasProperties)
      {
         if (usageName == null)
            throw new ArgumentNullException("usageName");

         IFCAnyHandleUtil.ValidateSubTypeOf(hasProperties, false, IFCEntityType.IfcProperty);
         ValidateProperty(name, description);

         IFCAnyHandle complexProperty = CreateInstance(file, IFCEntityType.IfcComplexProperty, null);
         IFCAnyHandleUtil.SetAttribute(complexProperty, "UsageName", usageName);
         IFCAnyHandleUtil.SetAttribute(complexProperty, "HasProperties", hasProperties);
         SetProperty(complexProperty, name, description);
         return complexProperty;
      }

      /// <summary>
      /// Creates a handle representing an IfcElementQuantity and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="methodOfMeasurement">Name of the method of measurement used to calculate the element quantity.</param>
      /// <param name="quantities">The individual quantities for the element.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateElementQuantity(IFCFile file, string guid, IFCAnyHandle ownerHistory,
          string name, string description, string methodOfMeasurement, HashSet<IFCAnyHandle> quantities)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(quantities, false, IFCEntityType.IfcPhysicalQuantity);
         ValidatePropertySetDefinition(guid, ownerHistory);

         IFCAnyHandle elementQuantity = CreateInstance(file, IFCEntityType.IfcElementQuantity, null);
         IFCAnyHandleUtil.SetAttribute(elementQuantity, "MethodOfMeasurement", methodOfMeasurement);
         IFCAnyHandleUtil.SetAttribute(elementQuantity, "Quantities", quantities);
         SetPropertySetDefinition(elementQuantity, guid, ownerHistory, name, description);
         return elementQuantity;
      }

      /// <summary>
      /// Creates an IfcOrganization and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="id">The identifier.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="actorRoles">Roles played by the organization.</param>
      /// <param name="addresses">Postal and telecom addresses of an organization.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateOrganization(IFCFile file, string id, string name, string description,
          IList<IFCAnyHandle> actorRoles, IList<IFCAnyHandle> addresses)
      {
         string organizationName = (name != null) ? name : String.Empty;

         IFCAnyHandleUtil.ValidateSubTypeOf(actorRoles, true, IFCEntityType.IfcActorRole);
         IFCAnyHandleUtil.ValidateSubTypeOf(addresses, true, IFCEntityType.IfcAddress);

         string idAttribName = (ExporterCacheManager.ExportOptionsCache.ExportAs4) ? "Identifier" : "Id";

         IFCAnyHandle organization = CreateInstance(file, IFCEntityType.IfcOrganization, null);
         IFCAnyHandleUtil.SetAttribute(organization, idAttribName, id);
         IFCAnyHandleUtil.SetAttribute(organization, "Name", organizationName);
         IFCAnyHandleUtil.SetAttribute(organization, "Description", description);
         IFCAnyHandleUtil.SetAttribute(organization, "Roles", actorRoles);
         IFCAnyHandleUtil.SetAttribute(organization, "Addresses", addresses);
         return organization;
      }

      /// <summary>
      /// Creates an IfcApplication and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="organization">The organization.</param>
      /// <param name="version">The version.</param>
      /// <param name="fullName">The full name.</param>
      /// <param name="identifier">The identifier.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateApplication(IFCFile file, IFCAnyHandle organization, string version, string fullName, string identifier)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(organization, false, IFCEntityType.IfcOrganization);
         if (version == null)
            throw new ArgumentNullException("version");
         if (fullName == null)
            throw new ArgumentNullException("fullName");
         if (identifier == null)
            throw new ArgumentNullException("identifier");

         IFCAnyHandle application = CreateInstance(file, IFCEntityType.IfcApplication, null);
         IFCAnyHandleUtil.SetAttribute(application, "ApplicationDeveloper", organization);
         IFCAnyHandleUtil.SetAttribute(application, "Version", version);
         IFCAnyHandleUtil.SetAttribute(application, "ApplicationFullName", fullName);
         IFCAnyHandleUtil.SetAttribute(application, "ApplicationIdentifier", identifier);
         return application;
      }

      /// <summary>
      /// Creates a handle representing an IfcDirection and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="directionRatios">The components in the direction of X axis, of Y axis and of Z axis.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateDirection(IFCFile file, IList<double> directionRatios)
      {
         IFCAnyHandle direction = CreateInstance(file, IFCEntityType.IfcDirection, null);
         IFCAnyHandleUtil.SetAttribute(direction, "DirectionRatios", directionRatios);
         return direction;
      }

      /// <summary>
      /// Creates an IfcGeometricRepresentationContext, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="identifier">The identifier.</param>
      /// <param name="type">The description of the type of a representation context.</param>
      /// <param name="dimension">The integer dimension count of the coordinate space modeled in a geometric representation context.</param>
      /// <param name="precision">Value of the model precision for geometric models.</param>
      /// <param name="worldCoordinateSystem">Establishment of the engineering coordinate system (often referred to as the world coordinate system in CAD)
      /// for all representation contexts used by the project.</param>
      /// <param name="trueNorth">Direction of the true north relative to the underlying coordinate system.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateGeometricRepresentationContext(IFCFile file, string identifier, string type, int dimension,
          double? precision, IFCAnyHandle worldCoordinateSystem, IFCAnyHandle trueNorth)
      {
         ValidateGeometricRepresentationContext(identifier, type, dimension, precision, worldCoordinateSystem, trueNorth);

         IFCAnyHandle geometricRepresentationContext = CreateInstance(file, IFCEntityType.IfcGeometricRepresentationContext, null);
         SetGeometricRepresentationContext(geometricRepresentationContext, identifier, type, dimension, precision, worldCoordinateSystem,
             trueNorth);
         return geometricRepresentationContext;
      }

      /// <summary>
      /// Creates an IfcGeometricRepresentationSubContext, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="identifier">The identifier.</param>
      /// <param name="type">The description of the type of a representation context.</param>
      /// <param name="parentContext">Parent context from which the sub context derives its world coordinate system, precision, space coordinate dimension and true north.</param>
      /// <param name="targetScale">The target plot scale of the representation to which this representation context applies.</param>
      /// <param name="targetView">Target view of the representation to which this representation context applies.</param>
      /// <param name="userDefinedTargetView">User defined target view, this value shall be given, if the targetView is set to UserDefined.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateGeometricRepresentationSubContext(IFCFile file,
          string identifier, string type, IFCAnyHandle parentContext, double? targetScale,
          IFCGeometricProjection targetView, string userDefinedTargetView)
      {
         ValidateRepresentationContext(identifier, type);
         IFCAnyHandleUtil.ValidateSubTypeOf(parentContext, false, IFCEntityType.IfcGeometricRepresentationContext);

         IFCAnyHandle geometricRepresentationSubContext = CreateInstance(file, IFCEntityType.IfcGeometricRepresentationSubContext, null);
         IFCAnyHandleUtil.SetAttribute(geometricRepresentationSubContext, "ParentContext", parentContext);
         IFCAnyHandleUtil.SetAttribute(geometricRepresentationSubContext, "TargetScale", targetScale);
         IFCAnyHandleUtil.SetAttribute(geometricRepresentationSubContext, "TargetView", targetView);
         IFCAnyHandleUtil.SetAttribute(geometricRepresentationSubContext, "UserDefinedTargetView", userDefinedTargetView);
         SetRepresentationContext(geometricRepresentationSubContext, identifier, type);
         return geometricRepresentationSubContext;
      }

      /// <summary>
      /// Creates a handle representing an IfcGeometricCurveSet and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="geometryElements">The collection of curve elements.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateGeometricCurveSet(IFCFile file, HashSet<IFCAnyHandle> geometryElements)
      {
         ValidateGeometricSet(geometryElements);
         IFCAnyHandle geometricCurveSet = CreateInstance(file, IFCEntityType.IfcGeometricCurveSet, null);
         SetGeometricSet(geometricCurveSet, geometryElements);
         return geometricCurveSet;
      }

      /// <summary>
      /// Creates a handle representing an IfcGeometricSet and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="geometryElements">The collection of curve elements.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateGeometricSet(IFCFile file, HashSet<IFCAnyHandle> geometryElements)
      {
         ValidateGeometricSet(geometryElements);
         IFCAnyHandle geometricSet = CreateInstance(file, IFCEntityType.IfcGeometricSet, null);
         SetGeometricSet(geometricSet, geometryElements);
         return geometricSet;
      }

      /// <summary>
      /// Creates an IfcPerson, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="identifier">Identification of the person.</param>
      /// <param name="familyName">The name by which the family identity of the person may be recognized.</param>
      /// <param name="givenName">The name by which a person is known within a family and by which he or she may be familiarly recognized.</param>
      /// <param name="middleNames">Additional names given to a person that enable their identification apart from others
      /// who may have the same or similar family and given names.</param>
      /// <param name="prefixTitles">The word, or group of words, which specify the person's social and/or professional standing and appear before his/her names.</param>
      /// <param name="suffixTitles">The word, or group of words, which specify the person's social and/or professional standing and appear after his/her names.</param>
      /// <param name="actorRoles">Roles played by the person.</param>
      /// <param name="addresses">Postal and telecommunication addresses of a person.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePerson(IFCFile file, string identifier, string familyName, string givenName,
          IList<string> middleNames, IList<string> prefixTitles, IList<string> suffixTitles,
          IList<IFCAnyHandle> actorRoles, IList<IFCAnyHandle> addresses)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(actorRoles, true, IFCEntityType.IfcActorRole);
         IFCAnyHandleUtil.ValidateSubTypeOf(addresses, true, IFCEntityType.IfcAddress);

         string idAttribName = (ExporterCacheManager.ExportOptionsCache.ExportAs4) ? "Identifier" : "Id";

         IFCAnyHandle person = CreateInstance(file, IFCEntityType.IfcPerson, null);
         IFCAnyHandleUtil.SetAttribute(person, idAttribName, identifier);
         IFCAnyHandleUtil.SetAttribute(person, "FamilyName", familyName);
         IFCAnyHandleUtil.SetAttribute(person, "GivenName", givenName);
         IFCAnyHandleUtil.SetAttribute(person, "MiddleNames", middleNames);
         IFCAnyHandleUtil.SetAttribute(person, "PrefixTitles", prefixTitles);
         IFCAnyHandleUtil.SetAttribute(person, "SuffixTitles", suffixTitles);
         IFCAnyHandleUtil.SetAttribute(person, "Roles", actorRoles);
         IFCAnyHandleUtil.SetAttribute(person, "Addresses", addresses);
         return person;
      }

      /// <summary>
      /// Creates an IfcPersonAndOrganization, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="person">The person who is related to the organization.</param>
      /// <param name="organization">The organization to which the person is related.</param>
      /// <param name="actorRoles">Roles played by the person within the context of an organization.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePersonAndOrganization(IFCFile file, IFCAnyHandle person, IFCAnyHandle organization,
          IList<IFCAnyHandle> actorRoles)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(person, false, IFCEntityType.IfcPerson);
         IFCAnyHandleUtil.ValidateSubTypeOf(organization, false, IFCEntityType.IfcOrganization);
         IFCAnyHandleUtil.ValidateSubTypeOf(actorRoles, true, IFCEntityType.IfcActorRole);

         IFCAnyHandle personAndOrganization = CreateInstance(file, IFCEntityType.IfcPersonAndOrganization, null);
         IFCAnyHandleUtil.SetAttribute(personAndOrganization, "ThePerson", person);
         IFCAnyHandleUtil.SetAttribute(personAndOrganization, "TheOrganization", organization);
         IFCAnyHandleUtil.SetAttribute(personAndOrganization, "Roles", actorRoles);
         return personAndOrganization;
      }

      /// <summary>
      /// Creates an IfcOwnerHistory, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="owningUser">Direct reference to the end user who currently "owns" this object.</param>
      /// <param name="owningApplication">Direct reference to the application which currently "Owns" this object on behalf of the owning user, who uses this application.</param>
      /// <param name="state">Enumeration that defines the current access state of the object.</param>
      /// <param name="changeAction">Enumeration that defines the actions associated with changes made to the object.</param>
      /// <param name="lastModifiedDate">Date and Time at which the last modification occurred.</param>
      /// <param name="lastModifyingUser">User who carried out the last modification.</param>
      /// <param name="lastModifyingApplication">Application used to carry out the last modification.</param>
      /// <param name="creationDate">Time and date of creation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateOwnerHistory(IFCFile file, IFCAnyHandle owningUser, IFCAnyHandle owningApplication,
          IFCState? state, IFCChangeAction changeAction, int? lastModifiedDate, IFCAnyHandle lastModifyingUser,
          IFCAnyHandle lastModifyingApplication, int creationDate)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(owningUser, false, IFCEntityType.IfcPersonAndOrganization);
         IFCAnyHandleUtil.ValidateSubTypeOf(owningApplication, false, IFCEntityType.IfcApplication);
         IFCAnyHandleUtil.ValidateSubTypeOf(lastModifyingUser, true, IFCEntityType.IfcPersonAndOrganization);
         IFCAnyHandleUtil.ValidateSubTypeOf(lastModifyingApplication, true, IFCEntityType.IfcApplication);

         IFCAnyHandle ownerHistory = CreateInstance(file, IFCEntityType.IfcOwnerHistory, null);
         IFCAnyHandleUtil.SetAttribute(ownerHistory, "OwningUser", owningUser);
         IFCAnyHandleUtil.SetAttribute(ownerHistory, "OwningApplication", owningApplication);
         IFCAnyHandleUtil.SetAttribute(ownerHistory, "State", state);
         IFCAnyHandleUtil.SetAttribute(ownerHistory, "ChangeAction", changeAction);
         IFCAnyHandleUtil.SetAttribute(ownerHistory, "LastModifiedDate", lastModifiedDate);
         IFCAnyHandleUtil.SetAttribute(ownerHistory, "LastModifyingUser", lastModifyingUser);
         IFCAnyHandleUtil.SetAttribute(ownerHistory, "LastModifyingApplication", lastModifyingApplication);
         IFCAnyHandleUtil.SetAttribute(ownerHistory, "CreationDate", creationDate);
         return ownerHistory;
      }

      /// <summary>
      /// Creates an IfcPostalAddress, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="purpose">Identifies the logical location of the address.</param>
      /// <param name="description">Text that relates the nature of the address.</param>
      /// <param name="userDefinedPurpose">Allows for specification of user specific purpose of the address.</param>
      /// <param name="internalLocation">An organization defined address for internal mail delivery.</param>
      /// <param name="addressLines">The postal address.</param>
      /// <param name="postalBox">An address that is implied by an identifiable mail drop.</param>
      /// <param name="town">The name of a town.</param>
      /// <param name="region">The name of a region.</param>
      /// <param name="postalCode">The code that is used by the country's postal service.</param>
      /// <param name="country">The name of a country.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePostalAddress(IFCFile file, IFCAddressType? purpose, string description, string userDefinedPurpose,
          string internalLocation, IList<string> addressLines, string postalBox, string town, string region,
          string postalCode, string country)
      {
         ValidateAddress(purpose, description, userDefinedPurpose);

         IFCAnyHandle postalAddress = CreateInstance(file, IFCEntityType.IfcPostalAddress, null);
         IFCAnyHandleUtil.SetAttribute(postalAddress, "InternalLocation", internalLocation);
         IFCAnyHandleUtil.SetAttribute(postalAddress, "AddressLines", addressLines);
         IFCAnyHandleUtil.SetAttribute(postalAddress, "PostalBox", postalBox);
         IFCAnyHandleUtil.SetAttribute(postalAddress, "Town", town);
         IFCAnyHandleUtil.SetAttribute(postalAddress, "Region", region);
         IFCAnyHandleUtil.SetAttribute(postalAddress, "PostalCode", postalCode);
         IFCAnyHandleUtil.SetAttribute(postalAddress, "Country", country);
         SetAddress(postalAddress, purpose, description, userDefinedPurpose);
         return postalAddress;
      }

      /// <summary>
      /// Creates an IfcPostalAddress, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="purpose">Identifies the logical location of the address.</param>
      /// <param name="description">Text that relates the nature of the address.</param>
      /// <param name="userDefinedPurpose">Allows for specification of user specific purpose of the address.</param>
      /// <param name="telephoneNumbers">An optional list of telephone numbers.</param>
      /// <param name="facsimileNumbers">An optional list of fax numbers.</param>
      /// <param name="pagerNumber">An optional pager number.</param>
      /// <param name="electronicMailAddresses">An optional list of e-mail addresses.</param>
      /// <param name="WWWHomePageURL">An optional URL.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateTelecomAddress(IFCFile file, IFCAddressType? purpose, string description,
          string userDefinedPurpose, IList<string> telephoneNumbers, IList<string> facsimileNumbers,
          string pagerNumber, IList<string> electronicMailAddresses, string WWWHomePageURL)
      {
         ValidateAddress(purpose, description, userDefinedPurpose);

         IFCAnyHandle telecomAddress = CreateInstance(file, IFCEntityType.IfcTelecomAddress, null);
         IFCAnyHandleUtil.SetAttribute(telecomAddress, "TelephoneNumbers", telephoneNumbers);
         IFCAnyHandleUtil.SetAttribute(telecomAddress, "FacsimileNumbers", facsimileNumbers);
         IFCAnyHandleUtil.SetAttribute(telecomAddress, "PagerNumber", pagerNumber);
         IFCAnyHandleUtil.SetAttribute(telecomAddress, "ElectronicMailAddresses", electronicMailAddresses);
         IFCAnyHandleUtil.SetAttribute(telecomAddress, "WWWHomePageURL", WWWHomePageURL);
         SetAddress(telecomAddress, purpose, description, userDefinedPurpose);
         return telecomAddress;
      }

      /// <summary>
      /// Creates a handle representing an IfcSIUnit and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="unitType">The type of the unit.</param>
      /// <param name="prefix">The SI Prefix for defining decimal multiples and submultiples of the unit.</param>
      /// <param name="name">The word, or group of words, by which the SI unit is referred to.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSIUnit(IFCFile file, IFCUnit unitType, IFCSIPrefix? prefix,
          IFCSIUnitName name)
      {
         IFCAnyHandle siUnit = CreateInstance(file, IFCEntityType.IfcSIUnit, null);
         IFCAnyHandleUtil.SetAttribute(siUnit, "Prefix", prefix);
         IFCAnyHandleUtil.SetAttribute(siUnit, "Name", name);
         SetNamedUnit(siUnit, null, unitType);
         return siUnit;
      }

      /// <summary>
      /// Creates a handle representing an IfcDerivedUnitElement and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="unit">The base unit.</param>
      /// <param name="exponent">The exponent of the base unit.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateDerivedUnitElement(IFCFile file, IFCAnyHandle unit, int exponent)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(unit, false, IFCEntityType.IfcNamedUnit);

         IFCAnyHandle derivedUnitElement = CreateInstance(file, IFCEntityType.IfcDerivedUnitElement, null);
         IFCAnyHandleUtil.SetAttribute(derivedUnitElement, "Unit", unit);
         IFCAnyHandleUtil.SetAttribute(derivedUnitElement, "Exponent", exponent);
         return derivedUnitElement;
      }

      /// <summary>
      /// Creates a handle representing an IfcDerivedUnit and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="elements">The derived unit elements of the unit.</param>
      /// <param name="unitType">The derived unit type.</param>
      /// <param name="userDefinedType">The word, or group of words, by which the derived unit is referred to.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateDerivedUnit(IFCFile file, ISet<IFCAnyHandle> elements, IFCDerivedUnitEnum unitType,
          string userDefinedType)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(elements, false, IFCEntityType.IfcDerivedUnitElement);

         IFCAnyHandle derivedUnit = CreateInstance(file, IFCEntityType.IfcDerivedUnit, null);
         IFCAnyHandleUtil.SetAttribute(derivedUnit, "Elements", elements);
         IFCAnyHandleUtil.SetAttribute(derivedUnit, "UnitType", unitType);
         IFCAnyHandleUtil.SetAttribute(derivedUnit, "UserDefinedType", userDefinedType);
         return derivedUnit;
      }

      /// <summary>
      /// Creates a handle representing an IfcDimensionalExponents and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="lengthExponent">The power of the length base quantity.</param>
      /// <param name="massExponent">The power of the mass base quantity.</param>
      /// <param name="timeExponent">The power of the time base quantity.</param>
      /// <param name="electricCurrentExponent">The power of the electric current base quantity.</param>
      /// <param name="thermodynamicTemperatureExponent">The power of the thermodynamic temperature base quantity.</param>
      /// <param name="amountOfSubstanceExponent">The power of the amount of substance base quantity.</param>
      /// <param name="luminousIntensityExponent">The power of the luminous intensity base quantity.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateDimensionalExponents(IFCFile file, int lengthExponent, int massExponent,
          int timeExponent, int electricCurrentExponent, int thermodynamicTemperatureExponent,
          int amountOfSubstanceExponent, int luminousIntensityExponent)
      {
         IFCAnyHandle dimensionalExponents = CreateInstance(file, IFCEntityType.IfcDimensionalExponents, null);
         IFCAnyHandleUtil.SetAttribute(dimensionalExponents, "LengthExponent", lengthExponent);
         IFCAnyHandleUtil.SetAttribute(dimensionalExponents, "MassExponent", massExponent);
         IFCAnyHandleUtil.SetAttribute(dimensionalExponents, "TimeExponent", timeExponent);
         IFCAnyHandleUtil.SetAttribute(dimensionalExponents, "ElectricCurrentExponent", electricCurrentExponent);
         IFCAnyHandleUtil.SetAttribute(dimensionalExponents, "ThermodynamicTemperatureExponent", thermodynamicTemperatureExponent);
         IFCAnyHandleUtil.SetAttribute(dimensionalExponents, "AmountOfSubstanceExponent", amountOfSubstanceExponent);
         IFCAnyHandleUtil.SetAttribute(dimensionalExponents, "LuminousIntensityExponent", luminousIntensityExponent);
         return dimensionalExponents;
      }

      /// <summary>
      /// Creates a handle representing an IfcMeasureWithUnit and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="valueComponent">The value of the physical quantity when expressed in the specified units.</param>
      /// <param name="unitComponent">The unit in which the physical quantity is expressed.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateMeasureWithUnit(IFCFile file, IFCData valueComponent, IFCAnyHandle unitComponent)
      {
         if (valueComponent == null)
            throw new ArgumentNullException("valueComponent");

         IFCAnyHandleUtil.ValidateSubTypeOf(unitComponent, false, IFCEntityType.IfcDerivedUnit,
             IFCEntityType.IfcNamedUnit, IFCEntityType.IfcMonetaryUnit);

         IFCAnyHandle measureWithUnit = CreateInstance(file, IFCEntityType.IfcMeasureWithUnit, null);
         IFCAnyHandleUtil.SetAttribute(measureWithUnit, "ValueComponent", valueComponent);
         IFCAnyHandleUtil.SetAttribute(measureWithUnit, "UnitComponent", unitComponent);
         return measureWithUnit;
      }

      /// <summary>
      /// Creates a handle representing an IfcMonetaryUnit and assigns it to the file for IFC2x3 and before.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="currencyType">The type of the currency, as supported by IFC.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateMonetaryUnit2x3(IFCFile file, IFCCurrencyType currencyType)
      {
         IFCAnyHandle monetaryUnit = CreateInstance(file, IFCEntityType.IfcMonetaryUnit, null);
         IFCAnyHandleUtil.SetAttribute(monetaryUnit, "Currency", currencyType);
         return monetaryUnit;
      }

      /// <summary>
      /// Creates a handle representing an IfcMonetaryUnit and assigns it to the file for IFC4 and beyond.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="currencyType">The type of the currency, as supported by IFC.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateMonetaryUnit4(IFCFile file, string currencyType)
      {
         IFCAnyHandle monetaryUnit = CreateInstance(file, IFCEntityType.IfcMonetaryUnit, null);
         IFCAnyHandleUtil.SetAttribute(monetaryUnit, "Currency", currencyType);
         return monetaryUnit;
      }

      /// <summary>
      /// Creates a handle representing an IfcConversionBasedUnit and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="dimensions">The dimensional exponents of the SI base units by which the named unit is defined.</param>
      /// <param name="unitType">The type of the unit.</param>
      /// <param name="name">The word, or group of words, by which the conversion based unit is referred to.</param>
      /// <param name="conversionFactor">The physical quantity from which the converted unit is derived.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateConversionBasedUnit(IFCFile file, IFCAnyHandle dimensions, IFCUnit unitType,
          string name, IFCAnyHandle conversionFactor)
      {
         ValidateNamedUnit(dimensions, unitType);

         IFCAnyHandleUtil.ValidateSubTypeOf(conversionFactor, false, IFCEntityType.IfcMeasureWithUnit);

         IFCAnyHandle conversionBasedUnit = CreateInstance(file, IFCEntityType.IfcConversionBasedUnit, null);
         IFCAnyHandleUtil.SetAttribute(conversionBasedUnit, "Name", name);
         IFCAnyHandleUtil.SetAttribute(conversionBasedUnit, "ConversionFactor", conversionFactor);
         SetNamedUnit(conversionBasedUnit, dimensions, unitType);
         return conversionBasedUnit;
      }

      /// <summary>
      /// Creates a handle representing an IfcUnitAssignment and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="units">Units to be included within a unit assignment.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateUnitAssignment(IFCFile file, HashSet<IFCAnyHandle> units)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(units, false, IFCEntityType.IfcDerivedUnit,
             IFCEntityType.IfcNamedUnit, IFCEntityType.IfcMonetaryUnit);

         IFCAnyHandle unitAssignment = CreateInstance(file, IFCEntityType.IfcUnitAssignment, null);
         IFCAnyHandleUtil.SetAttribute(unitAssignment, "Units", units);
         return unitAssignment;
      }

      /// <summary>
      /// Creates a handle representing an IfcCircle and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="position">The local coordinate system with the origin at the center of the circle.</param>
      /// <param name="radius">The radius of the circle.  Must be positive.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateCircle(IFCFile file, IFCAnyHandle position, double radius)
      {
         ValidateConic(position);

         if (radius < MathUtil.Eps())
            throw new ArgumentException("Radius is tiny, zero, or negative.");

         IFCAnyHandle circle = CreateInstance(file, IFCEntityType.IfcCircle, null);
         SetConic(circle, position);
         IFCAnyHandleUtil.SetAttribute(circle, "Radius", radius);
         return circle;
      }

      /// <summary>
      /// Creates a handle representing an IfcEllipse and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="position">The local coordinate system with the origin at the center of the circle.</param>
      /// <param name="semiAxis1">The radius in the direction of X in the local coordinate system.</param>
      /// <param name="semiAxis2">The radius in the direction of Y in the local coordinate system.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateEllipse(IFCFile file, IFCAnyHandle position, double semiAxis1, double semiAxis2)
      {
         ValidateConic(position);

         if (semiAxis1 < MathUtil.Eps())
            throw new ArgumentException("semiAxis1 is tiny, zero, or negative.");
         if (semiAxis2 < MathUtil.Eps())
            throw new ArgumentException("semiAxis2 is tiny, zero, or negative.");

         IFCAnyHandle ellipse = CreateInstance(file, IFCEntityType.IfcEllipse, null);
         SetConic(ellipse, position);
         IFCAnyHandleUtil.SetAttribute(ellipse, "SemiAxis1", semiAxis1);
         IFCAnyHandleUtil.SetAttribute(ellipse, "SemiAxis2", semiAxis2);
         return ellipse;
      }

      /// <summary>
      /// Creates a handle representing an IfcCartesianPoint and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="coordinates">The coordinates of the point locations.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateCartesianPoint(IFCFile file, IList<double> coordinates)
      {
         if (coordinates == null)
            throw new ArgumentNullException("coordinates");

         IFCAnyHandle cartesianPoint = CreateInstance(file, IFCEntityType.IfcCartesianPoint, null);
         IFCAnyHandleUtil.SetAttribute(cartesianPoint, "Coordinates", coordinates);
         return cartesianPoint;
      }

      /// <summary>
      /// Creates a handle representing IfcVertexPoint and assigns it to the file
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="coordinates">The coordinates of the vertex point</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateVertexPoint(IFCFile file, IList<double> coordinates)
      {
         if (coordinates == null)
            throw new ArgumentNullException("coordinates");

         IFCAnyHandle vertexGeometry = CreateCartesianPoint(file, coordinates);
         IFCAnyHandle vertexPoint = CreateInstance(file, IFCEntityType.IfcVertexPoint, null);
         IFCAnyHandleUtil.SetAttribute(vertexPoint, "VertexGeometry", vertexGeometry);
         return vertexPoint;
      }

      /// <summary>
      /// Creates a handle representing IfcVertexPoint by IfcPoint as an input instead of the list and assigns it to the file
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="point">IfcPoint</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateVertexPoint(IFCFile file, IFCAnyHandle point)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(point, false, IFCEntityType.IfcPoint);

         IFCAnyHandle vertexPoint = CreateInstance(file, IFCEntityType.IfcVertexPoint, null);
         IFCAnyHandleUtil.SetAttribute(vertexPoint, "VertexGeometry", point);
         return vertexPoint;
      }

      /// <summary>
      /// Creates a handle representing IfcEdgeCurve and assigns it to the file
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="edgeStart">start vertex of the edge</param>
      /// <param name="edgeEnd">end vertex of the edge</param>
      /// <param name="edgeGeometry">basis curve of the edge</param>
      /// <param name="sameSense">sense agreement of the edge and the curve</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateEdgeCurve(IFCFile file, IFCAnyHandle edgeStart, IFCAnyHandle edgeEnd, IFCAnyHandle edgeGeometry, bool sameSense)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(edgeStart, false, IFCEntityType.IfcVertex);
         IFCAnyHandleUtil.ValidateSubTypeOf(edgeEnd, false, IFCEntityType.IfcVertex);
         IFCAnyHandleUtil.ValidateSubTypeOf(edgeGeometry, false, IFCEntityType.IfcCurve);

         IFCAnyHandle edgeCurve = CreateInstance(file, IFCEntityType.IfcEdgeCurve, null);
         IFCAnyHandleUtil.SetAttribute(edgeCurve, "EdgeStart", edgeStart);
         IFCAnyHandleUtil.SetAttribute(edgeCurve, "EdgeEnd", edgeEnd);
         IFCAnyHandleUtil.SetAttribute(edgeCurve, "EdgeGeometry", edgeGeometry);
         IFCAnyHandleUtil.SetAttribute(edgeCurve, "SameSense", sameSense);

         return edgeCurve;
      }

      /// <summary>
      /// Creates a handle representing IfcOrientedEdge and assigns it tot the file
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="edgeElement">the edge element</param>
      /// <param name="orientation">the topological orientation of the edge and the vertices</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateOrientedEdge(IFCFile file, IFCAnyHandle edgeElement, bool orientation)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(edgeElement, false, IFCEntityType.IfcEdge);

         IFCAnyHandle orientedEdge = CreateInstance(file, IFCEntityType.IfcOrientedEdge, null);
         IFCAnyHandleUtil.SetAttribute(orientedEdge, "EdgeElement", edgeElement);
         IFCAnyHandleUtil.SetAttribute(orientedEdge, "Orientation", orientation);

         return orientedEdge;
      }

      /// <summary>
      /// Creates a handle representing IfcEdgeLoop and assigns it to the file
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="orientedEdgeList">List of the oriented edges</param>
      /// <returns>teh handle</returns>
      public static IFCAnyHandle CreateEdgeLoop(IFCFile file, IList<IFCAnyHandle> orientedEdgeList)
      {
         if (orientedEdgeList == null)
            throw new ArgumentNullException("EdgeLIst");

         foreach (IFCAnyHandle orientedEdge in orientedEdgeList)
            IFCAnyHandleUtil.ValidateSubTypeOf(orientedEdge, false, IFCEntityType.IfcOrientedEdge);

         IFCAnyHandle edgeLoop = CreateInstance(file, IFCEntityType.IfcEdgeLoop, null);
         IFCAnyHandleUtil.SetAttribute(edgeLoop, "EdgeList", orientedEdgeList);

         return edgeLoop;
      }

      /// <summary>
      /// Creates a handle representing IfcLine and assigns it to the file
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="coordinates">coordinates point reference of the line</param>
      /// <param name="vector">the vector direction</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateLine(IFCFile file, IList<double> coordinates, IFCAnyHandle vector)
      {
         if (coordinates == null)
            throw new ArgumentNullException("Coordinates");

         IFCAnyHandleUtil.ValidateSubTypeOf(vector, false, IFCEntityType.IfcVector);

         IFCAnyHandle line = CreateInstance(file, IFCEntityType.IfcLine, null);
         IFCAnyHandle pnt = CreateCartesianPoint(file, coordinates);
         IFCAnyHandleUtil.SetAttribute(line, "Pnt", pnt);
         IFCAnyHandleUtil.SetAttribute(line, "Dir", vector);

         return line;
      }

      /// <summary>
      /// Creates a handle representing IfcLine taking in IfcCartesianPoint as input, and assigns it to the file
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="point">the point (IfcCartesianPoint)</param>
      /// <param name="vector">the vector direction (IfcVector)</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateLine(IFCFile file, IFCAnyHandle point, IFCAnyHandle vector)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(point, false, IFCEntityType.IfcCartesianPoint);
         IFCAnyHandleUtil.ValidateSubTypeOf(vector, false, IFCEntityType.IfcVector);

         IFCAnyHandle line = CreateInstance(file, IFCEntityType.IfcLine, null);
         IFCAnyHandleUtil.SetAttribute(line, "Pnt", point);
         IFCAnyHandleUtil.SetAttribute(line, "Dir", vector);

         return line;
      }

      /// <summary>
      /// Creates a handle representing IfcVector and assignes it to the file
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="orientation">vector direction</param>
      /// <param name="magnitude">magnitude of the vector</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateVector(IFCFile file, IFCAnyHandle orientation, double magnitude)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(orientation, false, IFCEntityType.IfcDirection);

         IFCAnyHandle vector = CreateInstance(file, IFCEntityType.IfcVector, null);
         IFCAnyHandleUtil.SetAttribute(vector, "Orientation", orientation);
         IFCAnyHandleUtil.SetAttribute(vector, "Magnitude", magnitude);

         return vector;
      }

      /// <summary>
      /// Create IFC instance of IfcCartesianPointList2D
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="coordinateList">the list of the 2D coordinates</param>
      /// <returns>The handle</returns>
      public static IFCAnyHandle CreateCartesianPointList2D(IFCFile file, IList<IList<double>> coordinateList)
      {
         ValidateListOfList(coordinateList, false, "CoordinateList");

         IFCAnyHandle CreateCartesianPointList2D = CreateInstance(file, IFCEntityType.IfcCartesianPointList2D, null);
         IFCAnyHandleUtil.SetAttribute(CreateCartesianPointList2D, "CoordList", coordinateList, 1, null, 2, 2);

         return CreateCartesianPointList2D;
      }

      /// <summary>
      /// Create IFC instance of IfcCartesianPointList3D
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="coordinateList">the list of the 3D coordinates</param>
      /// <returns>The handle</returns>
      public static IFCAnyHandle CreateCartesianPointList3D(IFCFile file, IList<IList<double>> coordinateList)
      {
         ValidateListOfList(coordinateList, false, "CoordinateList");

         IFCAnyHandle CreateCartesianPointList3D = CreateInstance(file, IFCEntityType.IfcCartesianPointList3D, null);
         IFCAnyHandleUtil.SetAttribute(CreateCartesianPointList3D, "CoordList", coordinateList, 1, null, 3, 3);

         return CreateCartesianPointList3D;
      }

      public static IFCData CreateLineIndexType(IFCFile file, IList<int> lineIndexList)
      {
         if (lineIndexList == null || lineIndexList.Count == 0)
            throw new ArgumentException("The index is empty.", "IfcLineIndex");
         if (lineIndexList.Count < 2)
            throw new ArgumentException("The index must contains 2 or more members.", "IfcLineIndex");

         IFCAggregate lineIndex = null;
         foreach (int index in lineIndexList)
            lineIndex.Add(IFCData.CreateInteger(index));

         IFCData lineIndexData = IFCData.CreateIFCAggregate(lineIndex);
         return lineIndexData;
      }

      public static IFCData CreateArcIndexType(IFCFile file, IList<int> arcIndexList)
      {
         if (arcIndexList == null || arcIndexList.Count == 0)
            throw new ArgumentException("The index is empty.", "IfcArcIndex");
         if (arcIndexList.Count != 3)
            throw new ArgumentException("The index must contains exactly 3 members.", "IfcArcIndex");

         IFCAggregate arcIndex = null;
         foreach (int index in arcIndexList)
            arcIndex.Add(IFCData.CreateInteger(index));

         IFCData arcIndexData = IFCData.CreateIFCAggregate(arcIndex);
         return arcIndexData;
      }

      public static IFCAnyHandle CreateIndexedPolyCurve(IFCFile file, IFCAnyHandle coordinates, IList<IList<int>> segmentIndexList, bool? selfIntersect)
      {
         if (coordinates == null)
            throw new ArgumentNullException("Points");
         IFCAnyHandleUtil.ValidateSubTypeOf(coordinates, false, IFCEntityType.IfcCartesianPointList);
         if (segmentIndexList != null && segmentIndexList.Count == 0)
            throw new ArgumentNullException("Segments");

         IFCAnyHandle indexedPolyCurveHnd = CreateInstance(file, IFCEntityType.IfcIndexedPolyCurve, null);
         IFCAnyHandleUtil.SetAttribute(indexedPolyCurveHnd, "Points", coordinates);
         if (segmentIndexList != null)
            IFCAnyHandleUtil.SetAttribute(indexedPolyCurveHnd, "Segments", segmentIndexList, 1, null, 2, null);
         IFCAnyHandleUtil.SetAttribute(indexedPolyCurveHnd, "SelfIntersect", selfIntersect);

         return indexedPolyCurveHnd;
      }

      /// <summary>
      /// Creates a handle representing an IfcPolyline and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="points">The coordinates of the vertices.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePolyline(IFCFile file, IList<IFCAnyHandle> points)
      {
         if (points == null)
            throw new ArgumentNullException("points");

         IFCAnyHandle polylineHnd = CreateInstance(file, IFCEntityType.IfcPolyline, null);
         IFCAnyHandleUtil.SetAttribute(polylineHnd, "Points", points);
         return polylineHnd;
      }

      /// <summary>
      /// Creates a handle representing an IfcTrimmedCurve and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="basisCurve">The base curve.</param>
      /// <param name="trim1">The cartesian point, parameter, or both of end 1.</param>
      /// <param name="trim2">The cartesian point, parameter, or both of end 2.</param>
      /// <param name="senseAgreement">True if the end points match the orientation of the curve.</param>
      /// <param name="masterRepresentation">An enum stating which trim parameters are available.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateTrimmedCurve(IFCFile file, IFCAnyHandle basisCurve,
          HashSet<IFCData> trim1, HashSet<IFCData> trim2, bool senseAgreement,
          IFCTrimmingPreference masterRepresentation)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(basisCurve, false, IFCEntityType.IfcCurve);

         IFCAnyHandle trimmedCurve = CreateInstance(file, IFCEntityType.IfcTrimmedCurve, null);
         IFCAnyHandleUtil.SetAttribute(trimmedCurve, "BasisCurve", basisCurve);
         IFCAnyHandleUtil.SetAttribute(trimmedCurve, "Trim1", trim1);
         IFCAnyHandleUtil.SetAttribute(trimmedCurve, "Trim2", trim2);
         IFCAnyHandleUtil.SetAttribute(trimmedCurve, "SenseAgreement", senseAgreement);
         IFCAnyHandleUtil.SetAttribute(trimmedCurve, "MasterRepresentation", masterRepresentation);
         return trimmedCurve;
      }

      /// <summary>
      /// Creates a handle representing an IfcPolyLoop and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="polygon">The coordinates of the vertices.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePolyLoop(IFCFile file, IList<IFCAnyHandle> polygon)
      {
         if (polygon == null)
            throw new ArgumentNullException("polygon");

         if (polygon.Count < 3)
            throw new InvalidOperationException("IfcPolyLoop has fewer than 3 vertices, ignoring.");

         IFCAnyHandle polyLoop = CreateInstance(file, IFCEntityType.IfcPolyLoop, null);
         IFCAnyHandleUtil.SetAttribute(polyLoop, "Polygon", polygon);
         return polyLoop;
      }

      /// <summary>
      /// Creates a handle representing an IfcCompositeCurveSegment and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="transitionCode">TheThe continuity between curve segments.</param>
      /// <param name="sameSense">True if the segment has the same orientation as the IfcCompositeCurve.</param>
      /// <param name="parentCurve">The curve segment geometry.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateCompositeCurveSegment(IFCFile file, IFCTransitionCode transitionCode, bool sameSense,
          IFCAnyHandle parentCurve)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(parentCurve, false, IFCEntityType.IfcBoundedCurve);

         IFCAnyHandle compositeCurveSegment = CreateInstance(file, IFCEntityType.IfcCompositeCurveSegment, null);
         IFCAnyHandleUtil.SetAttribute(compositeCurveSegment, "Transition", transitionCode);
         IFCAnyHandleUtil.SetAttribute(compositeCurveSegment, "SameSense", sameSense);
         IFCAnyHandleUtil.SetAttribute(compositeCurveSegment, "ParentCurve", parentCurve);
         return compositeCurveSegment;
      }

      /// <summary>
      /// Creates a handle representing an IfcCompositeCurve and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="segments">The list of IfcCompositeCurveSegments.</param>
      /// <param name="selfIntersect">True if curve self-intersects, false if not, or unknown.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateCompositeCurve(IFCFile file, IList<IFCAnyHandle> segments, IFCLogical selfIntersect)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(segments, true, IFCEntityType.IfcCompositeCurveSegment);

         IFCAnyHandle compositeCurve = CreateInstance(file, IFCEntityType.IfcCompositeCurve, null);
         IFCAnyHandleUtil.SetAttribute(compositeCurve, "Segments", segments);
         IFCAnyHandleUtil.SetAttribute(compositeCurve, "SelfIntersect", selfIntersect);
         return compositeCurve;
      }

      /// <summary>
      /// Creates a handle representing an IfcSweptDiskSolid and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="directrix">The curve used to define the sweeping operation.</param>
      /// <param name="radius">The radius of the circular disk to be swept along the directrix.</param>
      /// <param name="innerRadius">This attribute is optional, if present it defines the radius of a circular hole in the centre of the disk.</param>
      /// <param name="startParam">The parameter value on the directrix at which the sweeping operation commences.</param>
      /// <param name="endParam">The parameter value on the directrix at which the sweeping operation ends.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSweptDiskSolid(IFCFile file, IFCAnyHandle directrix, double radius,
          double? innerRadius, double startParam, double endParam)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(directrix, false, IFCEntityType.IfcCurve);

         IFCAnyHandle sweptDiskSolid = file.CreateInstance(IFCEntityType.IfcSweptDiskSolid.ToString());
         IFCAnyHandleUtil.SetAttribute(sweptDiskSolid, "Directrix", directrix);
         IFCAnyHandleUtil.SetAttribute(sweptDiskSolid, "Radius", radius);
         IFCAnyHandleUtil.SetAttribute(sweptDiskSolid, "InnerRadius", innerRadius);
         IFCAnyHandleUtil.SetAttribute(sweptDiskSolid, "StartParam", startParam);
         IFCAnyHandleUtil.SetAttribute(sweptDiskSolid, "EndParam", endParam);
         return sweptDiskSolid;
      }

      private static void SetFaceBound(IFCAnyHandle faceBound, IFCAnyHandle bound, bool orientation)
      {
         IFCAnyHandleUtil.SetAttribute(faceBound, "Bound", bound);
         IFCAnyHandleUtil.SetAttribute(faceBound, "Orientation", orientation);
      }

      /// <summary>
      /// Creates a handle representing an IfcFaceBound and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="bound">The bounding loop.</param>
      /// <param name="orientation">The orientation of the face relative to the loop.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFaceBound(IFCFile file, IFCAnyHandle bound, bool orientation)
      {
         if (bound == null)
            throw new ArgumentNullException("bound");

         IFCAnyHandle faceBound = CreateInstance(file, IFCEntityType.IfcFaceBound, null);
         SetFaceBound(faceBound, bound, orientation);
         return faceBound;
      }

      /// <summary>
      /// Creates a handle representing an IfcFaceOuterBound and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="bound">The bounding loop.</param>
      /// <param name="orientation">The orientation of the face relative to the loop.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFaceOuterBound(IFCFile file, IFCAnyHandle bound, bool orientation)
      {
         if (bound == null)
            throw new ArgumentNullException("bound");

         IFCAnyHandle faceOuterBound = CreateInstance(file, IFCEntityType.IfcFaceOuterBound, null);
         SetFaceBound(faceOuterBound, bound, orientation);
         return faceOuterBound;
      }

      /// <summary>
      /// Creates a handle representing an IfcFace and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="bounds">The boundaries.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFace(IFCFile file, HashSet<IFCAnyHandle> bounds)
      {
         if (bounds == null)
            throw new ArgumentNullException("bound");
         if (bounds.Count == 0)
            throw new ArgumentException("no bounds for Face.");

         IFCAnyHandle face = CreateInstance(file, IFCEntityType.IfcFace, null);
         IFCAnyHandleUtil.SetAttribute(face, "Bounds", bounds);
         return face;
      }

      /// <summary>
      /// Creates a handle representing an IfcFaceSurface and assign it to the file
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="bounds">The boundaries</param>
      /// <param name="faceSurface">faceSurface</param>
      /// <param name="sameSense">same sense</param>
      /// <returns>the Handle</returns>
      public static IFCAnyHandle CreateFaceSurface(IFCFile file, HashSet<IFCAnyHandle> bounds, IFCAnyHandle faceSurface, bool sameSense)
      {
         if (bounds == null)
            throw new ArgumentNullException("bounds");
         if (bounds.Count == 0)
            throw new ArgumentException("no bounds for FaceSurface.");

         IFCAnyHandle fSurface = CreateInstance(file, IFCEntityType.IfcFaceSurface, null);
         IFCAnyHandleUtil.SetAttribute(fSurface, "Bounds", bounds);
         IFCAnyHandleUtil.SetAttribute(fSurface, "FaceSurface", faceSurface);
         IFCAnyHandleUtil.SetAttribute(fSurface, "SameSense", sameSense);
         return fSurface;
      }

      /// <summary>
      /// Creates a handle representing an IfcAdvancedFace and assign it to the file
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="bounds">The boundaries</param>
      /// <param name="faceSurface">faceSurface</param>
      /// <param name="sameSense">same sense</param>
      /// <returns>the Handle</returns>
      public static IFCAnyHandle CreateAdvancedFace(IFCFile file, HashSet<IFCAnyHandle> bounds, IFCAnyHandle faceSurface, bool sameSense)
      {
         if (bounds == null)
            throw new ArgumentNullException("bounds");
         if (bounds.Count == 0)
            throw new ArgumentException("no bounds for AdvancedFace.");

         IFCAnyHandle advancedFace = CreateInstance(file, IFCEntityType.IfcAdvancedFace, null);
         IFCAnyHandleUtil.SetAttribute(advancedFace, "Bounds", bounds);
         IFCAnyHandleUtil.SetAttribute(advancedFace, "FaceSurface", faceSurface);
         IFCAnyHandleUtil.SetAttribute(advancedFace, "SameSense", sameSense);
         return advancedFace;
      }

      /// <summary>
      /// Creates an IfcRepresentationMap, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="origin">The origin of the geometry.</param>
      /// <param name="representation">The geometry of the representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRepresentationMap(IFCFile file, IFCAnyHandle origin, IFCAnyHandle representation)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(origin, false, IFCEntityType.IfcAxis2Placement2D, IFCEntityType.IfcAxis2Placement3D);
         IFCAnyHandleUtil.ValidateSubTypeOf(representation, false, IFCEntityType.IfcRepresentation);

         IFCAnyHandle representationMap = CreateInstance(file, IFCEntityType.IfcRepresentationMap, null);
         IFCAnyHandleUtil.SetAttribute(representationMap, "MappingOrigin", origin);
         IFCAnyHandleUtil.SetAttribute(representationMap, "MappedRepresentation", representation);
         return representationMap;
      }

      /// <summary>
      /// Create a default IfcAxis1Placement and assign it to the file
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="location">The origin</param>
      /// <param name="axis">The Z direction</param>
      /// <returns>The handle</returns>
      public static IFCAnyHandle CreateAxis1Placement(IFCFile file, IFCAnyHandle location, IFCAnyHandle axis)
      {
         ValidatePlacement(location);
         IFCAnyHandleUtil.ValidateSubTypeOf(axis, true, IFCEntityType.IfcDirection);

         IFCAnyHandle axis1Placement = CreateInstance(file, IFCEntityType.IfcAxis1Placement, null);
         SetPlacement(axis1Placement, location);
         IFCAnyHandleUtil.SetAttribute(axis1Placement, "Axis", axis);
         return axis1Placement;
      }

      /// <summary>
      /// Creates a default IfcAxis2Placement2D, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="location">The origin.</param>
      /// <param name="axis">The Z direction.</param>
      /// <param name="refDirection">The X direction.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateAxis2Placement2D(IFCFile file, IFCAnyHandle location, IFCAnyHandle axis, IFCAnyHandle refDirection)
      {
         ValidatePlacement(location);
         IFCAnyHandleUtil.ValidateSubTypeOf(axis, true, IFCEntityType.IfcDirection);
         IFCAnyHandleUtil.ValidateSubTypeOf(refDirection, true, IFCEntityType.IfcDirection);

         IFCAnyHandle axis2Placement2D = CreateInstance(file, IFCEntityType.IfcAxis2Placement2D, null);
         IFCAnyHandleUtil.SetAttribute(axis2Placement2D, "Axis", axis);
         IFCAnyHandleUtil.SetAttribute(axis2Placement2D, "RefDirection", refDirection);
         SetPlacement(axis2Placement2D, location);
         return axis2Placement2D;
      }

      /// <summary>
      /// Creates a default IfcAxis2Placement3D, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="location">The origin.</param>
      /// <param name="axis">The Z direction.</param>
      /// <param name="refDirection">The X direction.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateAxis2Placement3D(IFCFile file, IFCAnyHandle location, IFCAnyHandle axis, IFCAnyHandle refDirection)
      {
         ValidatePlacement(location);
         IFCAnyHandleUtil.ValidateSubTypeOf(axis, true, IFCEntityType.IfcDirection);
         IFCAnyHandleUtil.ValidateSubTypeOf(refDirection, true, IFCEntityType.IfcDirection);

         IFCAnyHandle axis2Placement3D = CreateInstance(file, IFCEntityType.IfcAxis2Placement3D, null);
         IFCAnyHandleUtil.SetAttribute(axis2Placement3D, "Axis", axis);
         IFCAnyHandleUtil.SetAttribute(axis2Placement3D, "RefDirection", refDirection);
         SetPlacement(axis2Placement3D, location);
         return axis2Placement3D;
      }

      /// <summary>
      /// Creates an IfcBeam, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The geometric representation of the entity.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateBeam(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory, IFCAnyHandle objectPlacement,
          IFCAnyHandle representation, string preDefinedType)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle beam = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcBeam, element);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            SetSpecificEnumAttr(beam, "PredefinedType", preDefinedType, "IfcBeamType");
         }

         SetElement(exporterIFC, beam, element, guid, ownerHistory, objectPlacement, representation);
         return beam;
      }

      /// <summary>
      /// Create IfcBeamType, and assigns it to the file
      /// </summary>
      /// <param name="file">the File</param>
      /// <param name="guid">Globally Unique ID</param>
      /// <param name="ownerHistory">Owner History</param>
      /// <param name="name">Name</param>
      /// <param name="description">Description</param>
      /// <param name="applicableOccurrence">Applicable occurrence</param>
      /// <param name="propertySets">Assigned property sets</param>
      /// <param name="representationMaps">representation maps for mapped representation as a source</param>
      /// <param name="elementTag">tag</param>
      /// <param name="elementType">elementtype</param>
      /// <param name="predefinedType">predefined type</param>
      /// <returns></returns>
      public static IFCAnyHandle CreateBeamType(IFCFile file, Element revitType, HashSet<IFCAnyHandle> propertySets,
             IList<IFCAnyHandle> representationMaps, IFCBeamType predefinedType)
      {
         IFCAnyHandle beamType = CreateInstance(file, IFCEntityType.IfcBeamType, revitType);
         IFCAnyHandleUtil.SetAttribute(beamType, "PredefinedType", predefinedType);
         SetElementType(beamType, revitType, propertySets, representationMaps);
         return beamType;
      }

      /// <summary>
      /// Creates an IfcColumn, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The geometric representation of the entity.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateColumn(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory, IFCAnyHandle objectPlacement,
          IFCAnyHandle representation, string preDefinedType)
      {
         string validatedType = preDefinedType;

         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle column = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcColumn, element);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            SetSpecificEnumAttr(column, "PredefinedType", preDefinedType, "IfcColumnType");
         }

         SetElement(exporterIFC, column, element, guid, ownerHistory, objectPlacement, representation);
         return column;
      }

      /// <summary>
      /// Creates an IfcMechanicalFastener, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The geometric representation of the entity.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="nominalDiameter">The optinal nominal diameter.</param>
      /// <param name="nominalLength">The optional nominal length.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateMechanicalFastener(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, double? nominalDiameter, double? nominalLength, string preDefinedType)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle fastener = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcMechanicalFastener, element);
         SetElement(exporterIFC, fastener, element, guid, ownerHistory, objectPlacement, representation);

         string validatedType = preDefinedType;

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            // In IFC4 NominalDiameter and NominalLength attributes have been deprecated. PredefinedType attribute was added.
            SetSpecificEnumAttr(fastener, "PredefinedType", preDefinedType, "IfcMechanicalFastenerType");
         }
         else
         {
            IFCAnyHandleUtil.SetAttribute(fastener, "NominalDiameter", nominalDiameter);
            IFCAnyHandleUtil.SetAttribute(fastener, "NominalLength", nominalLength);
         }

         return fastener;
      }

      /// <summary>
      /// Creates an IfcMemberType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateMemberType(IFCFile file, Element revitType, HashSet<IFCAnyHandle> propertySets,
          IList<IFCAnyHandle> representationMaps, string predefinedType)
      {
         IFCAnyHandle memberType = CreateInstance(file, IFCEntityType.IfcMemberType, revitType);
         //IFCAnyHandleUtil.SetAttribute(memberType, "PredefinedType", predefinedType);
         SetSpecificEnumAttr(memberType, "PredefinedType", predefinedType, "IfcMemberType");
         SetElementType(memberType, revitType, propertySets, representationMaps);
         return memberType;
      }

      /// <summary>
      /// Creates an IfcFlowSegment, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The geometric representation of the entity.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFlowSegment(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle flowSegment = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcFlowSegment, element);
         SetElement(exporterIFC, flowSegment, element, guid, ownerHistory, objectPlacement, representation);
         return flowSegment;
      }

      /// <summary>
      /// Creates an IfcMember, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The geometric representation of the entity.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateMember(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string IFCEnumType)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle member = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcMember, element);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            SetSpecificEnumAttr(member, "PredefinedType", IFCEnumType, "IfcMemberType");
         }

         SetElement(exporterIFC, member, element, guid, ownerHistory, objectPlacement, representation);
         return member;
      }

      /// <summary>
      /// Creates an IfcPlate, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The geometric representation of the entity.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePlate(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string IFCEnumType)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle plate = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcPlate, element);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            SetSpecificEnumAttr(plate, "PredefinedType", IFCEnumType, "IfcPlateType");
         }

         SetElement(exporterIFC, plate, element, guid, ownerHistory, objectPlacement, representation);
         return plate;
      }

      /// <summary>
      /// Creates an IfcBeamType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateBeamType(IFCFile file, Element revitType, HashSet<IFCAnyHandle> propertySets,
          IList<IFCAnyHandle> representationMaps, string predefinedType)
      {
         IFCAnyHandle beamType = CreateInstance(file, IFCEntityType.IfcBeamType, revitType);
         SetSpecificEnumAttr(beamType, "PredefinedType", predefinedType, "IfcBeamType");
         
         SetElementType(beamType, revitType, propertySets, representationMaps);
         return beamType;
      }

      /// <summary>
      /// Creates an IfcColumnType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateColumnType(IFCFile file, Element revitType, HashSet<IFCAnyHandle> propertySets,
          IList<IFCAnyHandle> representationMaps, string predefinedType)
      {
         IFCAnyHandle columnType = CreateInstance(file, IFCEntityType.IfcColumnType, revitType);
         // IFCAnyHandleUtil.SetAttribute(columnType, "PredefinedType", predefinedType);
         SetSpecificEnumAttr(columnType, "PredefinedType", predefinedType, "IfcColumnType");
         SetElementType(columnType, revitType, propertySets, representationMaps);
         return columnType;
      }

      #region MEPObjects

      /// <summary>
      /// Get the name for the predefined type attribute, if it exists.
      /// </summary>
      /// <param name="entityType">The entity type.</param>
      /// <returns>The predefined type attribute, if it exists; null otherwise.</returns>
      /// <remarks>Before IFC4, some instance entities stored their predefined type in an attribute
      /// not called "PredefinedType".</remarks>
      public static string GetPredefinedTypeAttributeName(IFCEntityType entityType)
      {
         if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
         {
            if (entityType == IFCEntityType.IfcDistributionPort)
               return null;

            // The following have "PredefinedType", but are out of scope for now:
            // IfcCostSchedule, IfcOccupant, IfcProjectOrder, IfcProjectOrderRecord, IfcServiceLifeFactor
            // IfcStructuralAnalysisModel, IfcStructuralCurveMember, IfcStructuralLoadGroup, IfcStructuralSurfaceMember
            if ((entityType == IFCEntityType.IfcRamp) ||
                (entityType == IFCEntityType.IfcRoof) ||
                (entityType == IFCEntityType.IfcStair))
               return "ShapeType";
         }

         return "PredefinedType";
      }

      /// <summary>
      /// Creation of Generic IFC object, mainly used for MEP Objects as most of MEP objects are identical
      /// </summary>
      /// <param name="entityToCreate">The specific Entity (Enum) to create</param>
      /// <param name="file">The IFC file</param>
      /// <param name="guid">GUID</param>
      /// <param name="ownerHistory">Owner History</param>
      /// <param name="name">Name attribute</param>
      /// <param name="description">Description</param>
      /// <param name="objectType">ObjectType attribute</param>
      /// <param name="objectPlacement">Placement</param>
      /// <param name="representation">Geometry representation</param>
      /// <param name="elementTag">Element Tag attribue</param>
      /// <returns></returns>
      public static IFCAnyHandle CreateGenericIFCEntity(IFCExportInfoPair entityToCreate, ExporterIFC exporterIFC, Element element, string guid,
             IFCAnyHandle ownerHistory, IFCAnyHandle objectPlacement, IFCAnyHandle representation)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle genericIFCEntity = null;

         // There is no need to check for valid entity type because that has been enforced inside IFCExportInfoPair, only default to IfcBuildingElementProxy when it is UnKnown type
         if (entityToCreate.ExportInstance == IFCEntityType.UnKnown)
            genericIFCEntity = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcBuildingElementProxy, element);
         else
            genericIFCEntity = CreateInstance(exporterIFC.GetFile(), entityToCreate.ExportInstance, element);

         if (genericIFCEntity == null)
            return null;

         if (IFCAnyHandleUtil.IsSubTypeOf(genericIFCEntity, IFCEntityType.IfcElement))
         SetElement(exporterIFC, genericIFCEntity, element, guid, ownerHistory, objectPlacement, representation);

         if (!string.IsNullOrEmpty(entityToCreate.ValidatedPredefinedType))
         {
            // Earlier entities in IFC2x_ may not have PredefinedType property. Ignore error
            try
            {
               string predefinedTypeAttributeName = GetPredefinedTypeAttributeName(entityToCreate.ExportInstance);
               if (predefinedTypeAttributeName != null)
                  IFCAnyHandleUtil.SetAttribute(genericIFCEntity, predefinedTypeAttributeName, entityToCreate.ValidatedPredefinedType, true);
            }
            catch { }
         }

         return genericIFCEntity;
      }

      /// <summary>
      /// This is a generic create method for all IFC Type Objects, mainly for MEP objects
      /// </summary>
      /// <param name="typeEntityToCreate">Type entity to create</param>
      /// <param name="file">The IFC file</param>
      /// <param name="guid">GUID</param>
      /// <param name="ownerHistory">Owner History</param>
      /// <param name="name">name attribute</param>
      /// <param name="description">Description</param>
      /// <param name="applicableOccurrence">Applicable Occurence attribute</param>
      /// <param name="propertySets">Preperty Sets</param>
      /// <param name="representationMaps">RepresentationMap for geometry</param>
      /// <param name="elementTag">Element Tag</param>
      /// <param name="elementType">Element Type</param>
      /// <param name="predefinedType">preDefinedType</param>
      /// <returns></returns>
      public static IFCAnyHandle CreateGenericIFCType(IFCExportInfoPair typeEntityToCreate, Element revitType, IFCFile file,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, string predefinedType)
      {
         IFCAnyHandle genericIFCType = null;

         // No need to check the valid entity type. It has been enforced in IFCExportInfoPair. Rather create IfcBuildingElementTypeProxyType when the instance is IfcBuildingELementProxy and the type is UnKnown.
         // No type will be created if the Instance is Unknown too
         if (typeEntityToCreate.ExportType == IFCEntityType.UnKnown && typeEntityToCreate.ExportInstance == IFCEntityType.UnKnown)
            return null;

         // IfcBuildingElementProxyType is not supported in IFC2x2.
         if (typeEntityToCreate.ExportType == IFCEntityType.UnKnown && 
            typeEntityToCreate.ExportInstance == IFCEntityType.IfcBuildingElementProxy &&
            !ExporterCacheManager.ExportOptionsCache.ExportAs2x2)
            genericIFCType = CreateInstance(file, IFCEntityType.IfcBuildingElementProxyType, revitType);
         else if (typeEntityToCreate.ExportType != IFCEntityType.UnKnown)
            genericIFCType = CreateInstance(file, typeEntityToCreate.ExportType, revitType);

         if (genericIFCType == null)
            return null;

         SetElementType(genericIFCType, revitType, propertySets, representationMaps);

         if (!string.IsNullOrEmpty(predefinedType))
         {
            // Earlier types in IFC2x_ may not have PredefinedType property. Ignore error
            try
            {
               IFCAnyHandleUtil.SetAttribute(genericIFCType, "PredefinedType", predefinedType, true);
            }
            catch { }
         }

         return genericIFCType;
      }

      /// <summary>
      /// Creates an IfcElectricMotorType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateElectricMotorType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCElectricMotorType predefinedType)
      {
         IFCAnyHandle electricMotorType = CreateInstance(file, IFCEntityType.IfcElectricMotorType, revitType);
         IFCAnyHandleUtil.SetAttribute(electricMotorType, "PredefinedType", predefinedType);
         SetElementType(electricMotorType, revitType, propertySets, representationMaps);
         return electricMotorType;
      }

      /// <summary>
      /// Creates an IfcElectricTimeControlType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateElectricTimeControlType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCElectricTimeControlType predefinedType)
      {
         IFCAnyHandle electricTimeControlType = CreateInstance(file, IFCEntityType.IfcElectricTimeControlType, revitType);
         IFCAnyHandleUtil.SetAttribute(electricTimeControlType, "PredefinedType", predefinedType);
         SetElementType(electricTimeControlType, revitType, propertySets, representationMaps);
         return electricTimeControlType;
      }

      /// <summary>
      /// Creates an IfcEvaporativeCoolerType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateEvaporativeCoolerType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCEvaporativeCoolerType predefinedType)
      {
         IFCAnyHandle evaporativeCoolerType = CreateInstance(file, IFCEntityType.IfcEvaporativeCoolerType, revitType);
         IFCAnyHandleUtil.SetAttribute(evaporativeCoolerType, "PredefinedType", predefinedType);
         SetElementType(evaporativeCoolerType, revitType, propertySets, representationMaps);
         return evaporativeCoolerType;
      }

      /// <summary>
      /// Creates an IfcEvaporativeType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateEvaporatorType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCEvaporatorType predefinedType)
      {
         IFCAnyHandle evaporatorType = CreateInstance(file, IFCEntityType.IfcEvaporatorType, revitType);
         IFCAnyHandleUtil.SetAttribute(evaporatorType, "PredefinedType", predefinedType);
         SetElementType(evaporatorType, revitType, propertySets, representationMaps);
         return evaporatorType;
      }

      /// <summary>
      /// Creates an IfcFanType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFanType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCFanType predefinedType)
      {
         IFCAnyHandle fanType = CreateInstance(file, IFCEntityType.IfcFanType, revitType);
         IFCAnyHandleUtil.SetAttribute(fanType, "PredefinedType", predefinedType);
         SetElementType(fanType, revitType, propertySets, representationMaps);
         return fanType;
      }


      /// <summary>
      /// Creates an IfcFilterType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFilterType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCFilterType predefinedType)
      {
         IFCAnyHandle filterType = CreateInstance(file, IFCEntityType.IfcFilterType, revitType);
         IFCAnyHandleUtil.SetAttribute(filterType, "PredefinedType", predefinedType);
         SetElementType(filterType, revitType, propertySets, representationMaps);
         return filterType;
      }

      /// <summary>
      /// Creates an IfcFireSuppressionTerminalType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFireSuppressionTerminalType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCFireSuppressionTerminalType predefinedType)
      {
         IFCAnyHandle fireSuppressionTerminalType = CreateInstance(file, IFCEntityType.IfcFireSuppressionTerminalType, revitType);
         IFCAnyHandleUtil.SetAttribute(fireSuppressionTerminalType, "PredefinedType", predefinedType);
         SetElementType(fireSuppressionTerminalType, revitType, propertySets,
             representationMaps);
         return fireSuppressionTerminalType;
      }

      /// <summary>
      /// Creates an IfcFlowInstrumentType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFlowInstrumentType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCFlowInstrumentType predefinedType)
      {
         IFCAnyHandle flowInstrumentType = CreateInstance(file, IFCEntityType.IfcFlowInstrumentType, revitType);
         IFCAnyHandleUtil.SetAttribute(flowInstrumentType, "PredefinedType", predefinedType);
         SetElementType(flowInstrumentType, revitType, propertySets, representationMaps);
         return flowInstrumentType;
      }

      /// <summary>
      /// Creates an IfcFlowMeterType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFlowMeterType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCFlowMeterType predefinedType)
      {
         IFCAnyHandle flowMeterType = CreateInstance(file, IFCEntityType.IfcFlowMeterType, revitType);
         IFCAnyHandleUtil.SetAttribute(flowMeterType, "PredefinedType", predefinedType);
         SetElementType(flowMeterType, revitType, propertySets, representationMaps);
         return flowMeterType;
      }

      /// <summary>
      /// Creates an IfcGasTerminalType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateGasTerminalType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCGasTerminalType predefinedType)
      {
         IFCAnyHandle gasTerminalType = CreateInstance(file, IFCEntityType.IfcGasTerminalType, revitType);
         IFCAnyHandleUtil.SetAttribute(gasTerminalType, "PredefinedType", predefinedType);
         SetElementType(gasTerminalType, revitType, propertySets, representationMaps);
         return gasTerminalType;
      }

      /// <summary>
      /// Creates an IfcHeatExchangerType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateHeatExchangerType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCHeatExchangerType predefinedType)
      {
         IFCAnyHandle heatExchangerType = CreateInstance(file, IFCEntityType.IfcHeatExchangerType, revitType);
         IFCAnyHandleUtil.SetAttribute(heatExchangerType, "PredefinedType", predefinedType);
         SetElementType(heatExchangerType, revitType, propertySets, representationMaps);
         return heatExchangerType;
      }

      /// <summary>
      /// Creates an IfcHumidifierType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateHumidifierType(IFCFile file, Element revitType, HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCHumidifierType predefinedType)
      {
         IFCAnyHandle humidifierType = CreateInstance(file, IFCEntityType.IfcHumidifierType, revitType);
         IFCAnyHandleUtil.SetAttribute(humidifierType, "PredefinedType", predefinedType);
         SetElementType(humidifierType, revitType, propertySets, representationMaps);
         return humidifierType;
      }

      /// <summary>
      /// Creates an IfcJunctionBoxType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateJunctionBoxType(IFCFile file, Element revitType,
          string applicableOccurrence, HashSet<IFCAnyHandle> propertySets,
          IList<IFCAnyHandle> representationMaps, IFCJunctionBoxType predefinedType)
      {
         IFCAnyHandle junctionBoxType = CreateInstance(file, IFCEntityType.IfcJunctionBoxType, revitType);
         IFCAnyHandleUtil.SetAttribute(junctionBoxType, "PredefinedType", predefinedType);
         SetElementType(junctionBoxType, revitType, propertySets, representationMaps);
         return junctionBoxType;
      }

      /// <summary>
      /// Creates an IfcLampType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateLampType(IFCFile file, Element revitType,
          string applicableOccurrence, HashSet<IFCAnyHandle> propertySets,
          IList<IFCAnyHandle> representationMaps, string elementTag, string elementType, IFCLampType predefinedType)
      {
         IFCAnyHandle lampType = CreateInstance(file, IFCEntityType.IfcLampType, revitType);
         IFCAnyHandleUtil.SetAttribute(lampType, "PredefinedType", predefinedType);
         SetElementType(lampType, revitType, propertySets, representationMaps);
         return lampType;
      }

      /// <summary>
      /// Creates an IfcLightFixtureType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateLightFixtureType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCLightFixtureType predefinedType)
      {
         IFCAnyHandle lightFixtureType = CreateInstance(file, IFCEntityType.IfcLightFixtureType, revitType);
         IFCAnyHandleUtil.SetAttribute(lightFixtureType, "PredefinedType", predefinedType);
         SetElementType(lightFixtureType, revitType, propertySets, representationMaps);
         return lightFixtureType;
      }

      /// <summary>
      /// Creates an IfcMotorConnectionType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateMotorConnectionType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCMotorConnectionType predefinedType)
      {
         IFCAnyHandle motorConnectionType = CreateInstance(file, IFCEntityType.IfcMotorConnectionType, revitType);
         IFCAnyHandleUtil.SetAttribute(motorConnectionType, "PredefinedType", predefinedType);
         SetElementType(motorConnectionType, revitType, propertySets, representationMaps);
         return motorConnectionType;
      }

      /// <summary>
      /// Creates an IfcOutletType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateOutletType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets,
          IList<IFCAnyHandle> representationMaps, IFCOutletType predefinedType)
      {
         IFCAnyHandle outletType = CreateInstance(file, IFCEntityType.IfcOutletType, revitType);
         IFCAnyHandleUtil.SetAttribute(outletType, "PredefinedType", predefinedType);
         SetElementType(outletType, revitType, propertySets, representationMaps);
         return outletType;
      }

      /// <summary>
      /// Creates an IfcPipeFittingType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePipeFittingType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCPipeFittingType predefinedType)
      {
         IFCAnyHandle pipeFittingType = CreateInstance(file, IFCEntityType.IfcPipeFittingType, revitType);
         IFCAnyHandleUtil.SetAttribute(pipeFittingType, "PredefinedType", predefinedType);
         SetElementType(pipeFittingType, revitType, propertySets, representationMaps);
         return pipeFittingType;
      }

      /// <summary>
      /// Creates an IfcPipeSegmentType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePipeSegmentType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCPipeSegmentType predefinedType)
      {
         IFCAnyHandle pipeSegmentType = CreateInstance(file, IFCEntityType.IfcPipeSegmentType, revitType);
         IFCAnyHandleUtil.SetAttribute(pipeSegmentType, "PredefinedType", predefinedType);
         SetElementType(pipeSegmentType, revitType, propertySets, representationMaps);
         return pipeSegmentType;
      }

      /// <summary>
      /// Creates an IfcProtectiveDeviceType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateProtectiveDeviceType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCProtectiveDeviceType predefinedType)
      {
         IFCAnyHandle protectiveDeviceType = CreateInstance(file, IFCEntityType.IfcProtectiveDeviceType, revitType);
         IFCAnyHandleUtil.SetAttribute(protectiveDeviceType, "PredefinedType", predefinedType);
         SetElementType(protectiveDeviceType, revitType, propertySets, representationMaps);
         return protectiveDeviceType;
      }

      /// <summary>
      /// Creates an IfcPumpType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePumpType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCPumpType predefinedType)
      {
         IFCAnyHandle pumpType = CreateInstance(file, IFCEntityType.IfcPumpType, revitType);
         IFCAnyHandleUtil.SetAttribute(pumpType, "PredefinedType", predefinedType);
         SetElementType(pumpType, revitType, propertySets, representationMaps);
         return pumpType;
      }

      /// <summary>
      /// Creates an IfcSanitaryTerminalType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSanitaryTerminalType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCSanitaryTerminalType predefinedType)
      {
         IFCAnyHandle sanitaryTerminalType = CreateInstance(file, IFCEntityType.IfcSanitaryTerminalType, revitType);
         IFCAnyHandleUtil.SetAttribute(sanitaryTerminalType, "PredefinedType", predefinedType);
         SetElementType(sanitaryTerminalType, revitType, propertySets, representationMaps);
         return sanitaryTerminalType;
      }

      /// <summary>
      /// Creates an IfcSensorType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSensorType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCSensorType predefinedType)
      {
         IFCAnyHandle sensorType = CreateInstance(file, IFCEntityType.IfcSensorType, revitType);
         IFCAnyHandleUtil.SetAttribute(sensorType, "PredefinedType", predefinedType);
         SetElementType(sensorType, revitType, propertySets, representationMaps);
         return sensorType;
      }

      /// <summary>
      /// Creates an IfcSpaceHeaterType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSpaceHeaterType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCSpaceHeaterType predefinedType)
      {
         IFCAnyHandle spaceHeaterType = CreateInstance(file, IFCEntityType.IfcSpaceHeaterType, revitType);
         IFCAnyHandleUtil.SetAttribute(spaceHeaterType, "PredefinedType", predefinedType);
         SetElementType(spaceHeaterType, revitType, propertySets, representationMaps);
         return spaceHeaterType;
      }

      /// <summary>
      /// Creates an IfcStackTerminalType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateStackTerminalType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCStackTerminalType predefinedType)
      {
         IFCAnyHandle stackTerminalType = CreateInstance(file, IFCEntityType.IfcStackTerminalType, revitType);
         IFCAnyHandleUtil.SetAttribute(stackTerminalType, "PredefinedType", predefinedType);
         SetElementType(stackTerminalType, revitType, propertySets, representationMaps);
         return stackTerminalType;
      }

      /// <summary>
      /// Creates an IfcSwitchingDeviceType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSwitchingDeviceType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCSwitchingDeviceType predefinedType)
      {
         IFCAnyHandle switchingDeviceType = CreateInstance(file, IFCEntityType.IfcSwitchingDeviceType, revitType);
         IFCAnyHandleUtil.SetAttribute(switchingDeviceType, "PredefinedType", predefinedType);
         SetElementType(switchingDeviceType, revitType, propertySets, representationMaps);
         return switchingDeviceType;
      }

      /// <summary>
      /// Creates an IfcTankType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateTankType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCTankType predefinedType)
      {
         IFCAnyHandle tankType = CreateInstance(file, IFCEntityType.IfcTankType, revitType);
         IFCAnyHandleUtil.SetAttribute(tankType, "PredefinedType", predefinedType);
         SetElementType(tankType, revitType, propertySets, representationMaps);
         return tankType;
      }

      /// <summary>
      /// Creates an IfcTransformerType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateTransformerType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCTransformerType predefinedType)
      {
         IFCAnyHandle transformerType = CreateInstance(file, IFCEntityType.IfcTransformerType, revitType);
         IFCAnyHandleUtil.SetAttribute(transformerType, "PredefinedType", predefinedType);
         SetElementType(transformerType, revitType, propertySets, representationMaps);
         return transformerType;
      }

      /// <summary>
      /// Creates an IfcTubeBundleType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateTubeBundleType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCTubeBundleType predefinedType)
      {
         IFCAnyHandle tubeBundleType = CreateInstance(file, IFCEntityType.IfcTubeBundleType, revitType);
         IFCAnyHandleUtil.SetAttribute(tubeBundleType, "PredefinedType", predefinedType);
         SetElementType(tubeBundleType, revitType, propertySets, representationMaps);
         return tubeBundleType;
      }

      /// <summary>
      /// Creates an IfcUnitaryEquipmentType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateUnitaryEquipmentType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCUnitaryEquipmentType predefinedType)
      {
         IFCAnyHandle unitaryEquipmentType = CreateInstance(file, IFCEntityType.IfcUnitaryEquipmentType, revitType);
         IFCAnyHandleUtil.SetAttribute(unitaryEquipmentType, "PredefinedType", predefinedType);
         SetElementType(unitaryEquipmentType, revitType, propertySets, representationMaps);
         return unitaryEquipmentType;
      }

      /// <summary>
      /// Creates an IfcValveType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateValveType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCValveType predefinedType)
      {
         IFCAnyHandle valveType = CreateInstance(file, IFCEntityType.IfcValveType, revitType);
         IFCAnyHandleUtil.SetAttribute(valveType, "PredefinedType", predefinedType);
         SetElementType(valveType, revitType, propertySets, representationMaps);
         return valveType;
      }

      /// <summary>
      /// Creates an IfcWasteTerminalType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateWasteTerminalType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, IFCWasteTerminalType predefinedType)
      {
         IFCAnyHandle wasteTerminalType = CreateInstance(file, IFCEntityType.IfcWasteTerminalType, revitType);
         IFCAnyHandleUtil.SetAttribute(wasteTerminalType, "PredefinedType", predefinedType);
         SetElementType(wasteTerminalType, revitType, propertySets, representationMaps);
         return wasteTerminalType;
      }

      #endregion

      /// <summary>
      /// Creates an IfcFurnishingElementType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <returns>The handle.</returns>
      /// <remarks>Note that for CV2.0, this type is preferred over its sub-types. However, in IFC4 it is deprecated, and will be abstract in IFC5+.</remarks>
      public static IFCAnyHandle CreateFurnishingElementType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, string elementTag, string elementType)
      {
         IFCAnyHandle furnishingElementType = CreateInstance(file, IFCEntityType.IfcFurnishingElementType, revitType);
         SetElementType(furnishingElementType, revitType, propertySets, representationMaps);
         return furnishingElementType;
      }

      /// <summary>
      /// Creates an IfcFurnitureType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <param name="predefinedType">The predefined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFurnitureType(IFCFile file, Element revitType,
          HashSet<IFCAnyHandle> propertySets, IList<IFCAnyHandle> representationMaps, string elementTag, string elementType, string assemblyPlaceStr, string predefinedType)
      {
         IFCAnyHandle furnitureType = CreateInstance(file, IFCEntityType.IfcFurnitureType, revitType);

         if (string.IsNullOrEmpty(assemblyPlaceStr))
            assemblyPlaceStr = NamingUtil.GetOverrideStringValue(revitType, "IfcAssemblyPlace", null);

         if (!string.IsNullOrEmpty(assemblyPlaceStr))
         {
            IFCAssemblyPlace assemblyPlaceOverride = IFCAssemblyPlace.NotDefined;
            Enum.TryParse(assemblyPlaceStr, true, out assemblyPlaceOverride);
            if (assemblyPlaceOverride != IFCAssemblyPlace.NotDefined)
               assemblyPlaceStr = assemblyPlaceOverride.ToString();
         }
         else
            assemblyPlaceStr = "NOTDEFINED";
         IFCAnyHandleUtil.SetAttribute(furnitureType, "AssemblyPlace", assemblyPlaceStr, true);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            if (string.IsNullOrEmpty(predefinedType))
               predefinedType = "NOTDEFINED";
            predefinedType = IFCValidateEntry.GetValidIFCPredefinedTypeType(predefinedType, predefinedType, "IFCFurnitureType");
            IFCAnyHandleUtil.SetAttribute(furnitureType, "PredefinedType", predefinedType, true);
         }

         SetElementType(furnitureType, revitType, propertySets, representationMaps);
         return furnitureType;
      }

      /// <summary>
      /// Creates an IfcGroup, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateGroup(IFCFile file, string guid, IFCAnyHandle ownerHistory, string name,
          string description, string objectType)
      {
         ValidateGroup(guid, ownerHistory, name, objectType);

         IFCAnyHandle group = CreateInstance(file, IFCEntityType.IfcGroup, null);
         SetGroup(group, guid, ownerHistory, name, description, objectType);
         return group;
      }

      /// <summary>
      /// Creates an IfcElectricalCircuit, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <returns>The handle.</returns>
      /// <remarks>NOTE: this is deprecated in Coordination View 2.0, and missing from IFC4, so use sparingly.</remarks>
      public static IFCAnyHandle CreateElectricalCircuit(IFCFile file, string guid, IFCAnyHandle ownerHistory, string name,
          string description, string objectType)
      {
         ValidateSystem(guid, ownerHistory, name, objectType);

         IFCAnyHandle electricalCircuit = CreateInstance(file, IFCEntityType.IfcElectricalCircuit, null);
         SetSystem(electricalCircuit, guid, ownerHistory, name, description, objectType);
         return electricalCircuit;
      }

      /// <summary>
      /// Creates an IfcSystem, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSystem(IFCFile file, string guid, IFCAnyHandle ownerHistory, string name,
          string description, string objectType)
      {
         ValidateGroup(guid, ownerHistory, name, objectType);

         IFCAnyHandle system = CreateInstance(file, IFCEntityType.IfcSystem, null);
         SetGroup(system, guid, ownerHistory, name, description, objectType);
         return system;
      }

      /// <summary>
      /// Creates an IfcSystemFurnitureElementType, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="elementType">The type name.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSystemFurnitureElementType(IFCFile file, Element revitType, HashSet<IFCAnyHandle> propertySets,
          IList<IFCAnyHandle> representationMaps, string predefinedType)
      {
         IFCAnyHandle systemFurnitureElementType = CreateInstance(file, IFCEntityType.IfcSystemFurnitureElementType, revitType);
         SetElementType(systemFurnitureElementType, revitType, propertySets, representationMaps);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            if (!string.IsNullOrEmpty(predefinedType))
               IFCAnyHandleUtil.SetAttribute(systemFurnitureElementType, "PredefinedType", predefinedType, true);
         return systemFurnitureElementType;
      }

      /// <summary>
      /// Creates an IfcAnnotation and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The geometric representation of the entity.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateAnnotation(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation)
      {
         ValidateProduct(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle annotation = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcAnnotation, element);
         SetProduct(exporterIFC, annotation, element, guid, ownerHistory, objectPlacement, representation);
         return annotation;
      }

      /// <summary>
      /// Creates an IfcBuildingElementProxy, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The geometric representation of the entity.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="compositionType">The element composition of the proxy.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateBuildingElementProxy(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string compositionType)
      {
         string validatedType = compositionType;

         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle buildingElementProxy = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcBuildingElementProxy, element);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            validatedType = IFCValidateEntry.ValidateStrEnum<IFC4.IFCBuildingElementProxyType>(compositionType);
            IFCAnyHandleUtil.SetAttribute(buildingElementProxy, "preDefinedType", validatedType, true);
         }
         else
         {
            validatedType = IFCValidateEntry.ValidateStrEnum<IFCElementComposition>(compositionType);
            IFCAnyHandleUtil.SetAttribute(buildingElementProxy, "CompositionType", validatedType, true);
         }
         SetElement(exporterIFC, buildingElementProxy, element, guid, ownerHistory, objectPlacement, representation);
         return buildingElementProxy;
      }

      /// <summary>
      /// Creates an IfcCartesianTransformOperator3D, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="axis1">The X direction of the transformation coordinate system.</param>
      /// <param name="axis2">The Y direction of the transformation coordinate system.</param>
      /// <param name="localOrigin">The origin of the transformation coordinate system.</param>
      /// <param name="scale">The scale factor.</param>
      /// <param name="axis3">The Z direction of the transformation coordinate system.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateCartesianTransformationOperator3D(IFCFile file, IFCAnyHandle axis1, IFCAnyHandle axis2,
          IFCAnyHandle localOrigin, double? scale, IFCAnyHandle axis3)
      {
         ValidateCartesianTransformationOperator(axis1, axis2, localOrigin, scale);
         IFCAnyHandleUtil.ValidateSubTypeOf(axis3, true, IFCEntityType.IfcDirection);

         IFCAnyHandle cartesianTransformationOperator3D = CreateInstance(file, IFCEntityType.IfcCartesianTransformationOperator3D, null);
         IFCAnyHandleUtil.SetAttribute(cartesianTransformationOperator3D, "Axis3", axis3);
         SetCartesianTransformationOperator(cartesianTransformationOperator3D, axis1, axis2, localOrigin, scale);
         return cartesianTransformationOperator3D;
      }

      /// <summary>
      /// Creates an IfcColourRgb and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="name">The name.</param>
      /// <param name="red">The red colour component value.</param>
      /// <param name="green">The green colour component value.</param>
      /// <param name="blue">The blue colour component value.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateColourRgb(IFCFile file, string name, double red, double green, double blue)
      {
         IFCAnyHandle colourRgb = CreateInstance(file, IFCEntityType.IfcColourRgb, null);
         IFCAnyHandleUtil.SetAttribute(colourRgb, "Name", name);
         IFCAnyHandleUtil.SetAttribute(colourRgb, "Red", red);
         IFCAnyHandleUtil.SetAttribute(colourRgb, "Green", green);
         IFCAnyHandleUtil.SetAttribute(colourRgb, "Blue", blue);
         return colourRgb;
      }

      /// <summary>
      /// Creates an IfcConnectionSurfaceGeometry and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="surfaceOnRelatingElement">The handle to the surface on the relating element.  </param>
      /// <param name="surfaceOnRelatedElement">The handle to the surface on the related element.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateConnectionSurfaceGeometry(IFCFile file, IFCAnyHandle surfaceOnRelatingElement,
          IFCAnyHandle surfaceOnRelatedElement)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(surfaceOnRelatingElement, false, IFCEntityType.IfcSurface, IFCEntityType.IfcFaceSurface, IFCEntityType.IfcFaceBasedSurfaceModel);
         IFCAnyHandleUtil.ValidateSubTypeOf(surfaceOnRelatedElement, true, IFCEntityType.IfcSurface, IFCEntityType.IfcFaceSurface, IFCEntityType.IfcFaceBasedSurfaceModel);

         IFCAnyHandle connectionSurfaceGeometry = CreateInstance(file, IFCEntityType.IfcConnectionSurfaceGeometry, null);
         IFCAnyHandleUtil.SetAttribute(connectionSurfaceGeometry, "SurfaceOnRelatingElement", surfaceOnRelatingElement);
         IFCAnyHandleUtil.SetAttribute(connectionSurfaceGeometry, "SurfaceOnRelatedElement", surfaceOnRelatedElement);
         return connectionSurfaceGeometry;
      }

      /// <summary>
      /// Creates an IfcCurveBoundedPlane and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="basisSurface">The surface to be bound.</param>
      /// <param name="outerBoundary">The outer boundary of the surface.</param>
      /// <param name="innerBoundaries">An optional set of inner boundaries.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateCurveBoundedPlane(IFCFile file, IFCAnyHandle basisSurface, IFCAnyHandle outerBoundary,
          ISet<IFCAnyHandle> innerBoundaries)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(basisSurface, false, IFCEntityType.IfcPlane);
         IFCAnyHandleUtil.ValidateSubTypeOf(outerBoundary, false, IFCEntityType.IfcCurve);
         IFCAnyHandleUtil.ValidateSubTypeOf(innerBoundaries, true, IFCEntityType.IfcCurve);

         IFCAnyHandle curveBoundedPlane = CreateInstance(file, IFCEntityType.IfcCurveBoundedPlane, null);
         IFCAnyHandleUtil.SetAttribute(curveBoundedPlane, "BasisSurface", basisSurface);
         IFCAnyHandleUtil.SetAttribute(curveBoundedPlane, "OuterBoundary", outerBoundary);
         IFCAnyHandleUtil.SetAttribute(curveBoundedPlane, "InnerBoundaries", innerBoundaries);
         return curveBoundedPlane;
      }

      /// <summary>
      /// Creates an IfcCurveBoundedSurface and assign it to the file
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="basisSurface">The surface to be bound</param>
      /// <param name="boundaries">The curve boundaries</param>
      /// <param name="implicitOuter">Whether it uses implicit boundaries</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateCurveBoundedSurface(IFCFile file, IFCAnyHandle basisSurface, HashSet<IFCAnyHandle> boundaries, bool implicitOuter)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(basisSurface, false, IFCEntityType.IfcSurface);
         IFCAnyHandleUtil.ValidateSubTypeOf(boundaries, false, IFCEntityType.IfcBoundaryCurve);

         IFCAnyHandle curveBoundedSurface = CreateInstance(file, IFCEntityType.IfcCurveBoundedSurface, null);
         IFCAnyHandleUtil.SetAttribute(curveBoundedSurface, "BasisSurface", basisSurface);
         IFCAnyHandleUtil.SetAttribute(curveBoundedSurface, "Boundaries", boundaries);
         IFCAnyHandleUtil.SetAttribute(curveBoundedSurface, "ImplicitOuter", implicitOuter);
         return curveBoundedSurface;
      }

      /// <summary>
      /// Creates an IfcRectangularTrimmedSurface and assign it to the file
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="basisSurface">The surface to be bound/trimmed</param>
      /// <param name="u1">First u parametric value</param>
      /// <param name="v1">Fisrt v parametric value</param>
      /// <param name="u2">Second u parametric value</param>
      /// <param name="v2">Second v parametric value</param>
      /// <param name="uSense">direction sense of the first parameter of the trim surface compared to the u of the basissurface</param>
      /// <param name="vSense">direction sense of the second parameter of the trim surface compared to the v of the basissurface</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateRectangularTrimmedSurface(IFCFile file, IFCAnyHandle basisSurface, double u1, double v1, double u2, double v2,
                                  bool uSense, bool vSense)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(basisSurface, false, IFCEntityType.IfcSurface);

         IFCAnyHandle rectangularTrimmedSurface = CreateInstance(file, IFCEntityType.IfcRectangularTrimmedSurface, null);
         IFCAnyHandleUtil.SetAttribute(rectangularTrimmedSurface, "BasisSurface", basisSurface);
         IFCAnyHandleUtil.SetAttribute(rectangularTrimmedSurface, "U1", u1);
         IFCAnyHandleUtil.SetAttribute(rectangularTrimmedSurface, "V1", v1);
         IFCAnyHandleUtil.SetAttribute(rectangularTrimmedSurface, "U2", u2);
         IFCAnyHandleUtil.SetAttribute(rectangularTrimmedSurface, "V2", v2);
         IFCAnyHandleUtil.SetAttribute(rectangularTrimmedSurface, "Usense", uSense);
         IFCAnyHandleUtil.SetAttribute(rectangularTrimmedSurface, "Vsense", vSense);

         return rectangularTrimmedSurface;
      }

      /// <summary>
      /// Creates an IfcBSplineCurveWithKnots, and assigns it to the handle
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="degree">The degree of the b-spline curve</param>
      /// <param name="controlPointLists">The list of control points</param>
      /// <param name="curveForm">The form of the b-spline curve</param>
      /// <param name="closedCurve">The flag that indicates whether the curve is closed or not</param>
      /// <param name="selfIntersect">The flag that indicates whether the curve is self-intersect or not</param>
      /// <param name="knotMultiplicities">The knot multiplicities</param>
      /// <param name="knots">The knots</param>
      /// <param name="knotSpec">The type of the knots</param>
      /// <returns>The handle</returns>
      public static IFCAnyHandle CreateBSplineCurveWithKnots(IFCFile file, int degree, IList<IFCAnyHandle> controlPointLists, IFC4.IFCBSplineCurveForm curveForm,
          IFCLogical closedCurve, IFCLogical selfIntersect, IList<int> knotMultiplicities, IList<double> knots, IFC4.IFCKnotType knotSpec)
      {
         //TODO: validate parameters
         IFCAnyHandle bSplineCurveWithKnots = CreateInstance(file, IFCEntityType.IfcBSplineCurveWithKnots, null);
         SetBSplineCurveWithKnots(bSplineCurveWithKnots, degree, controlPointLists, curveForm, closedCurve, selfIntersect, knotMultiplicities, knots, knotSpec);
         return bSplineCurveWithKnots;
      }

      /// <summary>
      /// Creates an IfcRationalBSplineCurveWithKnots, and assigns it to the handle
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="degree">The degree of the b-spline curve</param>
      /// <param name="controlPointLists">The list of control points</param>
      /// <param name="curveForm">The form of the b-spline curve</param>
      /// <param name="closedCurve">The flag that indicates whether the curve is closed or not</param>
      /// <param name="selfIntersect">The flag that indicates whether the curve is self-intersect or not</param>
      /// <param name="knotMultiplicities">The knot multiplicities</param>
      /// <param name="knots">The knots</param>
      /// <param name="knotSpec">The type of the knots</param>
      /// <param name="weightsData">The weights</param>
      /// <returns>The handle</returns>
      public static IFCAnyHandle CreateRationalBSplineCurveWithKnots(IFCFile file, int degree, IList<IFCAnyHandle> controlPointLists, IFC4.IFCBSplineCurveForm curveForm,
          IFCLogical closedCurve, IFCLogical selfIntersect, IList<int> knotMultiplicities, IList<double> knots, IFC4.IFCKnotType knotSpec, IList<double> weightsData)
      {
         //TODO: validate parameters
         IFCAnyHandle rationBSplineCurveWithKnots = CreateInstance(file, IFCEntityType.IfcRationalBSplineCurveWithKnots, null);
         IFCAnyHandleUtil.SetAttribute(rationBSplineCurveWithKnots, "WeightsData", weightsData);
         SetBSplineCurveWithKnots(rationBSplineCurveWithKnots, degree, controlPointLists, curveForm, closedCurve, selfIntersect, knotMultiplicities, knots, knotSpec);
         return rationBSplineCurveWithKnots;
      }

      /// <summary>
      /// Sets the values of an IfcBSplineSurface.
      /// </summary>
      /// <param name="bsplineSurface">The IfcBSplineSurface entity.</param>
      /// <param name="uDegree">algebraic degree of basis functions in u</param>
      /// <param name="vDegree">algebraic degree of basis functions in v</param>
      /// <param name="controlPointsList">the list of control points</param>
      /// <param name="surfaceForm">enum of the surface type</param>
      /// <param name="uClosed">whether the surface is closed in the u direction</param>
      /// <param name="vClosed">whether the surface is closed in the v direction</param>
      /// <param name="selfIntersect">whether the surface is self-intersecting</param>
      private static void SetBSplineSurface(IFCAnyHandle bsplineSurface, int uDegree, int vDegree, IList<IList<IFCAnyHandle>> controlPointsList,
                      IFC4.IFCBSplineSurfaceForm surfaceForm, IFCLogical uClosed, IFCLogical vClosed, IFCLogical selfIntersect)
      {
         IFCAnyHandleUtil.SetAttribute(bsplineSurface, "UDegree", uDegree);
         IFCAnyHandleUtil.SetAttribute(bsplineSurface, "VDegree", vDegree);
         IFCAnyHandleUtil.SetAttribute(bsplineSurface, "ControlPointsList", controlPointsList, 2, null, 2, null);
         IFCAnyHandleUtil.SetAttribute(bsplineSurface, "SurfaceForm", surfaceForm);
         IFCAnyHandleUtil.SetAttribute(bsplineSurface, "UClosed", uClosed);
         IFCAnyHandleUtil.SetAttribute(bsplineSurface, "VClosed", vClosed);
         IFCAnyHandleUtil.SetAttribute(bsplineSurface, "SelfIntersect", selfIntersect);
      }

      /// <summary>
      /// Creates an IfcBSplineSurface and assigns it to the handle
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="uDegree">algebraic degree of basis functions in u</param>
      /// <param name="vDegree">algebraic degree of basis functions in v</param>
      /// <param name="controlPointsList">the list of control points</param>
      /// <param name="surfaceForm">enum of the surface type</param>
      /// <param name="uClosed">whether the surface is closed in the u direction</param>
      /// <param name="vClosed">whether the surface is closed in the v direction</param>
      /// <param name="selfIntersect">whether the surface is self-intersecting</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateBSplineSurface(IFCFile file, int uDegree, int vDegree, IList<IList<IFCAnyHandle>> controlPointsList,
                      IFC4.IFCBSplineSurfaceForm surfaceForm, IFCLogical uClosed, IFCLogical vClosed, IFCLogical selfIntersect)
      {
         ValidateControlPointsList(controlPointsList);

         IFCAnyHandle bSplineSurface = CreateInstance(file, IFCEntityType.IfcBSplineSurface, null);
         SetBSplineSurface(bSplineSurface, uDegree, vDegree, controlPointsList, surfaceForm, uClosed, vClosed, selfIntersect);

         return bSplineSurface;
      }

      /// <summary>
      /// Sets the values of an IfcBSplineSurfaceWithKnots.
      /// </summary>
      /// <param name="bSplineSurfaceWithKnots">The IfcBSplineSurfaceWithKnots handle.</param>
      /// <param name="uDegree">algebraic degree of basis functions in u</param>
      /// <param name="vDegree">algebraic degree of basis functions in v</param>
      /// <param name="controlPointsList">the list of control points</param>
      /// <param name="surfaceForm">enum of the surface type</param>
      /// <param name="uClosed">whether the surface is closed in the u direction</param>
      /// <param name="vClosed">whether the surface is closed in the v direction</param>
      /// <param name="selfIntersect">whether the surface is self-intersecting</param>
      /// <param name="uMultiplicities">The multiplicities of the knots in the u parameter direction</param>
      /// <param name="vMultiplicities">The multiplicities of the knots in the v parameter direction</param>
      /// <param name="uKnots">The list of the distinct knots in the u parameter direction</param>
      /// <param name="vKnots">The list of the distinct knots in the v parameter direction</param>
      /// <param name="knotSpec">The description of the knot type.</param>
      public static void SetBSplineSurfaceWithKnots(IFCAnyHandle bSplineSurfaceWithKnots, int uDegree, int vDegree, IList<IList<IFCAnyHandle>> controlPointsList,
                      IFC4.IFCBSplineSurfaceForm surfaceForm, IFCLogical uClosed, IFCLogical vClosed, IFCLogical selfIntersect, IList<int> uMultiplicities, IList<int> vMultiplicities,
                      IList<double> uKnots, IList<double> vKnots, IFC4.IFCKnotType knotSpec)
      {
         SetBSplineSurface(bSplineSurfaceWithKnots, uDegree, vDegree, controlPointsList, surfaceForm, uClosed, vClosed, selfIntersect);
         IFCAnyHandleUtil.SetAttribute(bSplineSurfaceWithKnots, "UMultiplicities", uMultiplicities);
         IFCAnyHandleUtil.SetAttribute(bSplineSurfaceWithKnots, "VMultiplicities", vMultiplicities);
         IFCAnyHandleUtil.SetAttribute(bSplineSurfaceWithKnots, "UKnots", uKnots);
         IFCAnyHandleUtil.SetAttribute(bSplineSurfaceWithKnots, "VKnots", vKnots);
         IFCAnyHandleUtil.SetAttribute(bSplineSurfaceWithKnots, "KnotSpec", knotSpec);
      }

      /// <summary>
      /// Creates an IfcBSplineSurfaceWithKnots and assigns it to the handle
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="uDegree">algebraic degree of basis functions in u</param>
      /// <param name="vDegree">algebraic degree of basis functions in v</param>
      /// <param name="controlPointsList">the list of control points</param>
      /// <param name="surfaceForm">enum of the surface type</param>
      /// <param name="uClosed">whether the surface is closed in the u direction</param>
      /// <param name="vClosed">whether the surface is closed in the v direction</param>
      /// <param name="selfIntersect">whether the surface is self-intersecting</param>
      /// <param name="uMultiplicities">The multiplicities of the knots in the u parameter direction</param>
      /// <param name="vMultiplicities">The multiplicities of the knots in the v parameter direction</param>
      /// <param name="uKnots">The list of the distinct knots in the u parameter direction</param>
      /// <param name="vKnots">The list of the distinct knots in the v parameter direction</param>
      /// <param name="knotSpec">The description of the knot type.</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateBSplineSurfaceWithKnots(IFCFile file, int uDegree, int vDegree, IList<IList<IFCAnyHandle>> controlPointsList,
                      IFC4.IFCBSplineSurfaceForm surfaceForm, IFCLogical uClosed, IFCLogical vClosed, IFCLogical selfIntersect, List<int> uMultiplicities, List<int> vMultiplicities,
                      List<double> uKnots, List<double> vKnots, IFC4.IFCKnotType knotSpec)
      {
         ValidateControlPointsList(controlPointsList);

         IFCAnyHandle bSplineSurfaceWithKnots = CreateInstance(file, IFCEntityType.IfcBSplineSurfaceWithKnots, null);
         SetBSplineSurfaceWithKnots(bSplineSurfaceWithKnots, uDegree, vDegree, controlPointsList, surfaceForm, uClosed, vClosed, selfIntersect,
            uMultiplicities, vMultiplicities, uKnots, vKnots, knotSpec);
         return bSplineSurfaceWithKnots;
      }

      /// <summary>
      /// Creates an IfcRationalBSplineSurfaceWithKnots and assigns it to the handle
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="uDegree">algebraic degree of basis functions in u</param>
      /// <param name="vDegree">algebraic degree of basis functions in v</param>
      /// <param name="controlPointsList">the list of control points</param>
      /// <param name="surfaceForm">enum of the surface type</param>
      /// <param name="uClosed">whether the surface is closed in the u direction</param>
      /// <param name="vClosed">whether the surface is closed in the v direction</param>
      /// <param name="selfIntersect">whether the surface is self-intersecting</param>
      /// <param name="uMultiplicities">The multiplicities of the knots in the u parameter direction</param>
      /// <param name="vMultiplicities">The multiplicities of the knots in the v parameter direction</param>
      /// <param name="uKnots">The list of the distinct knots in the u parameter direction</param>
      /// <param name="vKnots">The list of the distinct knots in the v parameter direction</param>
      /// <param name="knotSpec">The description of the knot type.</param>
      /// <param name="weightsData">The double array of weights for the control points.</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateRationalBSplineSurfaceWithKnots(IFCFile file, int uDegree, int vDegree, IList<IList<IFCAnyHandle>> controlPointsList,
                      IFC4.IFCBSplineSurfaceForm surfaceForm, IFCLogical uClosed, IFCLogical vClosed, IFCLogical selfIntersect, IList<int> uMultiplicities, IList<int> vMultiplicities,
                      IList<double> uKnots, IList<double> vKnots, IFC4.IFCKnotType knotSpec, IList<IList<double>> weightsData)
      {
         ValidateControlPointsList(controlPointsList);
         ValidateWeightsDataList(weightsData);

         IFCAnyHandle rationalBSplineSurfaceWithKnots = CreateInstance(file, IFCEntityType.IfcRationalBSplineSurfaceWithKnots, null);
         SetBSplineSurfaceWithKnots(rationalBSplineSurfaceWithKnots, uDegree, vDegree, controlPointsList, surfaceForm, uClosed, vClosed, selfIntersect,
             uMultiplicities, vMultiplicities, uKnots, vKnots, knotSpec);
         IFCAnyHandleUtil.SetAttribute(rationalBSplineSurfaceWithKnots, "WeightsData", weightsData, 2, null, 2, null);
         return rationalBSplineSurfaceWithKnots;
      }

      /// <summary>
      /// Creates an IfcDistributionControlElement, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The geometric representation of the entity.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="controlElementId">The ControlElement Point Identification assigned to this control element by the Building Automation System.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateDistributionControlElement(ExporterIFC exporterIFC, Element element,
          string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, string controlElementId)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle distributionControlElement = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcDistributionControlElement, element);
         // ControlElementId has been removed in IFC4 in favor of using Classification
         if (!ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            string ifcelementType = null;
            ParameterUtil.GetStringValueFromElement(element, "IfcElementType", out ifcelementType);
            IFCAnyHandleUtil.SetAttribute(distributionControlElement, "ControlElementId", controlElementId);
         }
         SetElement(exporterIFC, distributionControlElement, element, guid, ownerHistory, objectPlacement, representation);
         return distributionControlElement;
      }

      /// <summary>
      /// Creates an IfcDistributionElement, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The geometric representation of the entity.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateDistributionElement(ExporterIFC exporterIFC, Element element,
          string guid, IFCAnyHandle ownerHistory, IFCAnyHandle objectPlacement, IFCAnyHandle representation)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle distributionElement = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcDistributionElement, element);
         SetElement(exporterIFC, distributionElement, element, guid, ownerHistory, objectPlacement, representation);
         return distributionElement;
      }

      /// <summary>
      /// Creates an IfcDistributionPort and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The geometric representation of the entity.</param>
      /// <param name="flowDirection">The flow direction.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateDistributionPort(ExporterIFC exporterIFC, Element element,
          string guid, IFCAnyHandle ownerHistory, IFCAnyHandle objectPlacement, IFCAnyHandle representation, IFCFlowDirection? flowDirection)
      {
         ValidateProduct(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle distributionPort = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcDistributionPort, element);
         IFCAnyHandleUtil.SetAttribute(distributionPort, "FlowDirection", flowDirection);
         SetProduct(exporterIFC, distributionPort, element, guid, ownerHistory, objectPlacement, representation);

         return distributionPort;
      }

      /// <summary>
      /// Creates an IfcDoor, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The geometric representation of the entity.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="overallHeight">The height of the door.</param>
      /// <param name="overallWidth">The width of the door.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateDoor(ExporterIFC exporterIFC, Element element,
          string guid, IFCAnyHandle ownerHistory, IFCAnyHandle objectPlacement, IFCAnyHandle representation,
          double? overallHeight, double? overallWidth, string preDefinedType, string operationType, string userDefinedOperationType)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle door = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcDoor, element);
         IFCAnyHandleUtil.SetAttribute(door, "OverallHeight", overallHeight);
         IFCAnyHandleUtil.SetAttribute(door, "OverallWidth", overallWidth);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            string validatedPreDefinedType = IFCValidateEntry.ValidateStrEnum<IFC4.IFCDoorType>(preDefinedType);
            IFCAnyHandleUtil.SetAttribute(door, "PreDefinedType", validatedPreDefinedType, true);
            string validatedOperationType = IFCValidateEntry.ValidateStrEnum<IFC4.IFCDoorTypeOperation>(operationType);
            IFCAnyHandleUtil.SetAttribute(door, "OperationType", validatedOperationType, true);
            if (String.Compare(validatedOperationType, "USERDEFINED", true) == 0 && !string.IsNullOrEmpty(userDefinedOperationType))
               IFCAnyHandleUtil.SetAttribute(door, "UserDefinedOperationType", userDefinedOperationType);
         }
         SetElement(exporterIFC, door, element, guid, ownerHistory, objectPlacement, representation);
         return door;
      }

      /// <summary>
      /// Creates an IfcDoorLiningProperties, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="liningDepth">The depth of the lining.</param>
      /// <param name="liningThickness">The thickness of the lining.</param>
      /// <param name="thresholdDepth">The depth of the threshold.</param>
      /// <param name="thresholdThickness">The thickness of the threshold.</param>
      /// <param name="transomThickness">The thickness of the transom.</param>
      /// <param name="transomOffset">The offset of the transom.</param>
      /// <param name="liningOffset">The offset of the lining.</param>
      /// <param name="thresholdOffset">The offset of the threshold.</param>
      /// <param name="casingThickness">The thickness of the casing.</param>
      /// <param name="casingDepth">The depth of the casing.</param>
      /// <param name="shapeAspectStyle">The shape aspect for the door style.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateDoorLiningProperties(IFCFile file,
          string guid, IFCAnyHandle ownerHistory, string name, string description, double? liningDepth,
          double? liningThickness, double? thresholdDepth, double? thresholdThickness, double? transomThickness,
          double? transomOffset, double? liningOffset, double? thresholdOffset, double? casingThickness,
          double? casingDepth, IFCAnyHandle shapeAspectStyle)
      {
         ValidatePropertySetDefinition(guid, ownerHistory);
         IFCAnyHandleUtil.ValidateSubTypeOf(shapeAspectStyle, true, IFCEntityType.IfcShapeAspect);

         IFCAnyHandle doorLiningProperties = CreateInstance(file, IFCEntityType.IfcDoorLiningProperties, null);
         IFCAnyHandleUtil.SetAttribute(doorLiningProperties, "LiningDepth", liningDepth);
         IFCAnyHandleUtil.SetAttribute(doorLiningProperties, "LiningThickness", liningThickness);
         IFCAnyHandleUtil.SetAttribute(doorLiningProperties, "ThresholdDepth", thresholdDepth);
         IFCAnyHandleUtil.SetAttribute(doorLiningProperties, "ThresholdThickness", thresholdThickness);
         IFCAnyHandleUtil.SetAttribute(doorLiningProperties, "TransomThickness", transomThickness);
         IFCAnyHandleUtil.SetAttribute(doorLiningProperties, "TransomOffset", transomOffset);
         IFCAnyHandleUtil.SetAttribute(doorLiningProperties, "LiningOffset", liningOffset);
         IFCAnyHandleUtil.SetAttribute(doorLiningProperties, "ThresholdOffset", thresholdOffset);
         IFCAnyHandleUtil.SetAttribute(doorLiningProperties, "CasingThickness", casingThickness);
         IFCAnyHandleUtil.SetAttribute(doorLiningProperties, "CasingDepth", casingDepth);
         IFCAnyHandleUtil.SetAttribute(doorLiningProperties, "ShapeAspectStyle", shapeAspectStyle);
         SetPropertySetDefinition(doorLiningProperties, guid, ownerHistory, name, description);
         return doorLiningProperties;
      }

      /// <summary>
      /// Creates an IfcWindowLiningProperties, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="liningDepth">The depth of the lining.</param>
      /// <param name="liningThickness">The thickness of the lining.</param>
      /// <param name="transomThickness">The thickness of the transom(s).</param>
      /// <param name="mullionThickness">The thickness of the mullion(s).</param>
      /// <param name="firstTransomOffset">The offset of the first transom.</param>
      /// <param name="secondTransomOffset">The offset of the second transom.</param>
      /// <param name="firstMullionOffset">The offset of the first mullion.</param>
      /// <param name="secondMullionOffset">The offset of the second mullion.</param>
      /// <param name="shapeAspectStyle">The shape aspect for the window style.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateWindowLiningProperties(IFCFile file,
          string guid, IFCAnyHandle ownerHistory, string name, string description,
          double? liningDepth, double? liningThickness, double? transomThickness,
          double? mullionThickness, double? firstTransomOffset, double? secondTransomOffset,
          double? firstMullionOffset, double? secondMullionOffset, IFCAnyHandle shapeAspectStyle)
      {
         ValidatePropertySetDefinition(guid, ownerHistory);
         IFCAnyHandleUtil.ValidateSubTypeOf(shapeAspectStyle, true, IFCEntityType.IfcShapeAspect);

         IFCAnyHandle windowLiningProperties = CreateInstance(file, IFCEntityType.IfcWindowLiningProperties, null);
         IFCAnyHandleUtil.SetAttribute(windowLiningProperties, "LiningDepth", liningDepth);
         IFCAnyHandleUtil.SetAttribute(windowLiningProperties, "LiningThickness", liningThickness);
         IFCAnyHandleUtil.SetAttribute(windowLiningProperties, "TransomThickness", transomThickness);
         IFCAnyHandleUtil.SetAttribute(windowLiningProperties, "MullionThickness", mullionThickness);
         IFCAnyHandleUtil.SetAttribute(windowLiningProperties, "FirstTransomOffset", firstTransomOffset);
         IFCAnyHandleUtil.SetAttribute(windowLiningProperties, "SecondTransomOffset", secondTransomOffset);
         IFCAnyHandleUtil.SetAttribute(windowLiningProperties, "FirstMullionOffset", firstMullionOffset);
         IFCAnyHandleUtil.SetAttribute(windowLiningProperties, "SecondMullionOffset", secondMullionOffset);
         IFCAnyHandleUtil.SetAttribute(windowLiningProperties, "ShapeAspectStyle", shapeAspectStyle);
         SetPropertySetDefinition(windowLiningProperties, guid, ownerHistory, name, description);
         return windowLiningProperties;
      }

      /// <summary>
      /// Creates an IfcDoorPanelProperties, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="panelDepth">The depth of the panel.</param>
      /// <param name="panelOperation">The panel operation.</param>
      /// <param name="panelWidth">The width of the panel.</param>
      /// <param name="panelPosition">The panel position.</param>
      /// <param name="shapeAspectStyle">The shape aspect for the door style.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateDoorPanelProperties(IFCFile file,
          string guid, IFCAnyHandle ownerHistory, string name, string description, double? panelDepth,
          string panelOperation, double? panelWidth, string panelPosition, IFCAnyHandle shapeAspectStyle)
      {
         ValidatePropertySetDefinition(guid, ownerHistory);
         IFCAnyHandleUtil.ValidateSubTypeOf(shapeAspectStyle, true, IFCEntityType.IfcShapeAspect);

         IFCAnyHandle doorPanelProperties = CreateInstance(file, IFCEntityType.IfcDoorPanelProperties, null);
         IFCAnyHandleUtil.SetAttribute(doorPanelProperties, "PanelDepth", panelDepth);
         IFCAnyHandleUtil.SetAttribute(doorPanelProperties, "PanelWidth", panelWidth);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            panelOperation = IFCValidateEntry.ValidateStrEnum<IFC4.IFCDoorPanelOperation>(panelOperation);
            panelPosition = IFCValidateEntry.ValidateStrEnum<IFC4.IFCDoorPanelPosition>(panelPosition);
         }
         else
         {
            panelOperation = IFCValidateEntry.ValidateStrEnum<IFCDoorPanelOperation>(panelOperation);
            panelPosition = IFCValidateEntry.ValidateStrEnum<IFCDoorPanelPosition>(panelPosition);
         }
         IFCAnyHandleUtil.SetAttribute(doorPanelProperties, "PanelOperation", panelOperation, true);
         IFCAnyHandleUtil.SetAttribute(doorPanelProperties, "PanelPosition", panelPosition, true);

         IFCAnyHandleUtil.SetAttribute(doorPanelProperties, "ShapeAspectStyle", shapeAspectStyle);
         SetPropertySetDefinition(doorPanelProperties, guid, ownerHistory, name, description);
         return doorPanelProperties;
      }

      /// <summary>
      /// Creates an IfcWindowPanelProperties, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="operationType">The panel operation.</param>
      /// <param name="positionType">The panel position.</param>
      /// <param name="frameDepth">The depth of the frame.</param>
      /// <param name="frameThickness">The thickness of the frame.</param>
      /// <param name="shapeAspectStyle">The shape aspect for the window style.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateWindowPanelProperties(IFCFile file,
          string guid, IFCAnyHandle ownerHistory, string name, string description,
          IFCWindowPanelOperation operationType, IFCWindowPanelPosition positionType,
          double? frameDepth, double? frameThickness, IFCAnyHandle shapeAspectStyle)
      {
         ValidatePropertySetDefinition(guid, ownerHistory);
         IFCAnyHandleUtil.ValidateSubTypeOf(shapeAspectStyle, true, IFCEntityType.IfcShapeAspect);

         IFCAnyHandle windowPanelProperties = CreateInstance(file, IFCEntityType.IfcWindowPanelProperties, null);
         IFCAnyHandleUtil.SetAttribute(windowPanelProperties, "OperationType", operationType);
         IFCAnyHandleUtil.SetAttribute(windowPanelProperties, "PanelPosition", positionType);
         IFCAnyHandleUtil.SetAttribute(windowPanelProperties, "FrameDepth", frameDepth);
         IFCAnyHandleUtil.SetAttribute(windowPanelProperties, "FrameThickness", frameThickness);
         IFCAnyHandleUtil.SetAttribute(windowPanelProperties, "ShapeAspectStyle", shapeAspectStyle);
         SetPropertySetDefinition(windowPanelProperties, guid, ownerHistory, name, description);
         return windowPanelProperties;
      }

      /// <summary>
      /// Creates an IfcDoorStyle, and assigns it to the file. [DEPRECATED from IFC4 onward]
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="operationType">The operation type.</param>
      /// <param name="constructionType">The construction type.</param>
      /// <param name="parameterTakesPrecedence">True if the parameter given in the attached lining and panel properties exactly define the geometry,
      /// false if the attached style shape takes precedence.</param>
      /// <param name="sizeable">True if the attached IfcMappedRepresentation (if given) can be sized (using scale factor of transformation), false if not.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateDoorStyle(IFCFile file, Element typeRevit, HashSet<IFCAnyHandle> propertySets,
          IList<IFCAnyHandle> representationMaps, string operationType, IFCDoorStyleConstruction constructionType, bool parameterTakesPrecedence, bool sizeable)
      {
         ValidateTypeProduct("null", ExporterCacheManager.OwnerHistoryHandle, propertySets, representationMaps);

         IFCAnyHandle doorStyle = CreateInstance(file, IFCEntityType.IfcDoorStyle, typeRevit);
         IFCAnyHandleUtil.SetAttribute(doorStyle, "OperationType", operationType, true);
         IFCAnyHandleUtil.SetAttribute(doorStyle, "ConstructionType", constructionType);
         IFCAnyHandleUtil.SetAttribute(doorStyle, "ParameterTakesPrecedence", parameterTakesPrecedence);
         IFCAnyHandleUtil.SetAttribute(doorStyle, "Sizeable", sizeable);
         SetTypeProduct(doorStyle, typeRevit, propertySets, representationMaps);
         return doorStyle;
      }

      /// <summary>
      /// New in IFC4, replacing IFCDoorStyle that is now deprecated. Carries similar information as IFCDoorStyle with some changes
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="guid">the GUID</param>
      /// <param name="ownerHistory">the owner history</param>
      /// <param name="name">the name</param>
      /// <param name="description">the description</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="preDefinedType">Predefined generic type for a door that is specified in an enumeration.</param>
      /// <param name="operationType">the operation type</param>
      /// <param name="parameterTakesPrecedence">True if the parameter given in the attached lining and panel properties exactly define the geometry,
      /// false if the attached style shape takes precedence.</param>
      /// <param name="userDefinedOperationType">Designator for the user defined operation type, shall only be provided, if the value of OperationType is set to USERDEFINED.</param>
      /// <returns></returns>
      public static IFCAnyHandle CreateDoorType(IFCFile file, Element revitType, HashSet<IFCAnyHandle> propertySets,
          IList<IFCAnyHandle> representationMaps, string preDefinedType, string operationType,
          bool parameterTakesPrecedence, string userDefinedOperationType)
      {
         ValidateTypeProduct("null", ExporterCacheManager.OwnerHistoryHandle, propertySets, representationMaps);

         IFCAnyHandle doorType = CreateInstance(file, IFCEntityType.IfcDoorType, revitType);
         string validatedPreDefinedType = IFCValidateEntry.ValidateStrEnum<IFC4.IFCDoorType>(preDefinedType);
         IFCAnyHandleUtil.SetAttribute(doorType, "PreDefinedType", validatedPreDefinedType, true);
         string validatedOperationType = IFCValidateEntry.ValidateStrEnum<IFC4.IFCDoorTypeOperation>(operationType);
         IFCAnyHandleUtil.SetAttribute(doorType, "OperationType", validatedOperationType, true);
         IFCAnyHandleUtil.SetAttribute(doorType, "ParameterTakesPrecedence", parameterTakesPrecedence);
         if (String.Compare(validatedOperationType, "USERDEFINED", true) == 0 && !string.IsNullOrEmpty(userDefinedOperationType))
            IFCAnyHandleUtil.SetAttribute(doorType, "UserDefinedOperationType", userDefinedOperationType);
         SetTypeProduct(doorType, revitType, propertySets, representationMaps);
         return doorType;
      }

      /// <summary>
      /// Creates an IfcWindowStyle, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="operationType">The operation type.</param>
      /// <param name="constructionType">The construction type.</param>
      /// <param name="paramTakesPrecedence"> True if the parameter given in the attached lining and panel properties exactly define the geometry,
      /// false if the attached style shape takes precedence.</param>
      /// <param name="sizeable">True if the attached IfcMappedRepresentation (if given) can be sized (using scale factor of transformation), false if not.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateWindowStyle(IFCFile file, Element revitType, HashSet<IFCAnyHandle> propertySets,
          IList<IFCAnyHandle> representationMaps, IFCWindowStyleConstruction constructionType,
          Toolkit.IFCWindowStyleOperation operationType, bool paramTakesPrecedence, bool sizeable)
      {
         ValidateTypeProduct("null", ExporterCacheManager.OwnerHistoryHandle, propertySets, representationMaps);

         IFCAnyHandle windowStyle = CreateInstance(file, IFCEntityType.IfcWindowStyle, revitType);
         IFCAnyHandleUtil.SetAttribute(windowStyle, "ConstructionType", constructionType);
         IFCAnyHandleUtil.SetAttribute(windowStyle, "OperationType", operationType);
         IFCAnyHandleUtil.SetAttribute(windowStyle, "ParameterTakesPrecedence", paramTakesPrecedence);
         IFCAnyHandleUtil.SetAttribute(windowStyle, "Sizeable", sizeable);
         SetTypeProduct(windowStyle, revitType, propertySets, representationMaps);
         return windowStyle;
      }

      /// <summary>
      /// New in IFC4, replacing IFCWindowStyle that is now deprecated. Carries similar information as IFCWindowStyle with some changes
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="guid">the guid</param>
      /// <param name="ownerHistory">the owner history</param>
      /// <param name="name">the name</param>
      /// <param name="description">the description</param>
      /// <param name="applicableOccurrence">The attribute optionally defines the data type of the occurrence object.</param>
      /// <param name="propertySets">The property set(s) associated with the type.</param>
      /// <param name="representationMaps">The mapped geometries associated with the type.</param>
      /// <param name="elementTag">The tag that represents the entity.</param>
      /// <param name="preDefinedType">Identifies the predefined types of a window element from which the type required may be set.</param>
      /// <param name="partitioningType">Type defining the general layout of the window type in terms of the partitioning of panels.</param>
      /// <param name="paramTakesPrecedence">The Boolean value reflects, whether the parameter given in the attached lining and panel properties exactly define the geometry (TRUE), or whether the attached style shape take precedence (FALSE). In the last case the parameter have only informative value. If not provided, no such information can be infered. </param>
      /// <param name="userDefinedPartitioningType">Designator for the user defined partitioning type, shall only be provided, if the value of PartitioningType is set to USERDEFINED.</param>
      /// <returns></returns>
      public static IFCAnyHandle CreateWindowType(IFCFile file, Element revitType, HashSet<IFCAnyHandle> propertySets,
         IList<IFCAnyHandle> representationMaps, string preDefinedType,
         string partitioningType, bool paramTakesPrecedence, string userDefinedPartitioningType)
      {
         ValidateTypeProduct("null", ExporterCacheManager.OwnerHistoryHandle, propertySets, representationMaps);

         IFCAnyHandle windowType = CreateInstance(file, IFCEntityType.IfcWindowType, revitType);
         string validatedPreDefinedType = IFCValidateEntry.ValidateStrEnum<IFC4.IFCWindowType>(preDefinedType);
         IFCAnyHandleUtil.SetAttribute(windowType, "PreDefinedType", validatedPreDefinedType, true);
         string validatedPartitioningType = IFCValidateEntry.ValidateStrEnum<IFC4.IFCWindowTypePartitioning>(partitioningType);
         IFCAnyHandleUtil.SetAttribute(windowType, "PartitioningType", validatedPartitioningType, true);
         IFCAnyHandleUtil.SetAttribute(windowType, "ParameterTakesPrecedence", paramTakesPrecedence);
         if (String.Compare(validatedPartitioningType, "USERDEFINED", true) == 0 && !string.IsNullOrEmpty(userDefinedPartitioningType))
            IFCAnyHandleUtil.SetAttribute(windowType, "UserDefinedPartitioningType", userDefinedPartitioningType);
         SetTypeProduct(windowType, revitType, propertySets, representationMaps);
         return windowType;
      }

      /// <summary>
      /// Creates an IfcFacetedBrep and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="outer">The closed shell.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFacetedBrep(IFCFile file, IFCAnyHandle outer)
      {
         ValidateManifoldSolidBrep(outer);

         IFCAnyHandle facetedBrep = CreateInstance(file, IFCEntityType.IfcFacetedBrep, null);
         SetManifoldSolidBrep(facetedBrep, outer);
         return facetedBrep;
      }

      /// <summary>
      /// Create instance of AdvancedBrep RepresentationItem
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="outer">The outer closed shell</param>
      /// <returns>The handle</returns>
      public static IFCAnyHandle CreateAdvancedBrep(IFCFile file, IFCAnyHandle outer)
      {
         ValidateManifoldSolidBrep(outer);

         IFCAnyHandle advancedBrep = CreateInstance(file, IFCEntityType.IfcAdvancedBrep, null);
         SetManifoldSolidBrep(advancedBrep, outer);
         return advancedBrep;
      }

      /// <summary>
      /// Creates an IfcMappedItem, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="mappingSource">The mapped geometry.</param>
      /// <param name="mappingTarget">The transformation operator for this instance of the mapped geometry.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateMappedItem(IFCFile file, IFCAnyHandle mappingSource, IFCAnyHandle mappingTarget)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(mappingSource, false, IFCEntityType.IfcRepresentationMap);
         IFCAnyHandleUtil.ValidateSubTypeOf(mappingTarget, false, IFCEntityType.IfcCartesianTransformationOperator);

         IFCAnyHandle mappedItem = CreateInstance(file, IFCEntityType.IfcMappedItem, null);
         IFCAnyHandleUtil.SetAttribute(mappedItem, "MappingSource", mappingSource);
         IFCAnyHandleUtil.SetAttribute(mappedItem, "MappingTarget", mappingTarget);
         return mappedItem;
      }

      /// <summary>
      /// Creates an IfcMaterial and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="name">The name.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateMaterial(IFCFile file, string name, string description = null, string category = null)
      {
         if (name == null)
            throw new ArgumentNullException("name");

         IFCAnyHandle material = CreateInstance(file, IFCEntityType.IfcMaterial, null);
         IFCAnyHandleUtil.SetAttribute(material, "Name", name);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            if (!string.IsNullOrEmpty(description))
               IFCAnyHandleUtil.SetAttribute(material, "Description", description);
            if (!string.IsNullOrEmpty(category))
               IFCAnyHandleUtil.SetAttribute(material, "Category", category);
         }
         return material;
      }

      /// <summary>
      /// Creates an IfcMaterialList and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="materials">The list of materials.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateMaterialList(IFCFile file, IList<IFCAnyHandle> materials)
      {
         if (materials.Count == 0)
            throw new ArgumentNullException("materials");

         IFCAnyHandleUtil.ValidateSubTypeOf(materials, false, IFCEntityType.IfcMaterial);

         IFCAnyHandle materialList = CreateInstance(file, IFCEntityType.IfcMaterialList, null);
         IFCAnyHandleUtil.SetAttribute(materialList, "Materials", materials);
         return materialList;
      }

      /// <summary>
      /// Creates a handle representing an IfcMaterialDefinitionRepresentation and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="representations">The collection of representations assigned to the material.</param>
      /// <param name="representedMaterial">The material.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateMaterialDefinitionRepresentation(IFCFile file, string name, string description, IList<IFCAnyHandle> representations,
          IFCAnyHandle representedMaterial)
      {
         ValidateProductRepresentation(name, description, representations);
         IFCAnyHandleUtil.ValidateSubTypeOf(representedMaterial, false, IFCEntityType.IfcMaterial);

         IFCAnyHandle productDefinitionShape = CreateInstance(file, IFCEntityType.IfcMaterialDefinitionRepresentation, null);
         SetProductRepresentation(productDefinitionShape, name, description, representations);
         IFCAnyHandleUtil.SetAttribute(productDefinitionShape, "RepresentedMaterial", representedMaterial);

         return productDefinitionShape;
      }

      /// <summary>
      /// Creates an IfcMaterialLayer and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="material">The material.</param>
      /// <param name="layerThickness">The thickness of the layer.</param>
      /// <param name="isVentilated">  Indication of whether the material layer represents an air layer (or cavity).</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateMaterialLayer(IFCFile file, IFCAnyHandle material, double layerThickness, IFCLogical? isVentilated,
         string name = null, string description = null, string category = null, int? priority = null)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(material, true, IFCEntityType.IfcMaterial);

         IFCAnyHandle materialLayer = CreateInstance(file, IFCEntityType.IfcMaterialLayer, null);
         IFCAnyHandleUtil.SetAttribute(materialLayer, "Material", material);
         IFCAnyHandleUtil.SetAttribute(materialLayer, "LayerThickness", layerThickness);
         if (isVentilated.HasValue)
            IFCAnyHandleUtil.SetAttribute(materialLayer, "IsVentilated", isVentilated);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            if (!string.IsNullOrEmpty(name))
               IFCAnyHandleUtil.SetAttribute(materialLayer, "Name", name);
            if (!string.IsNullOrEmpty(description))
               IFCAnyHandleUtil.SetAttribute(materialLayer, "Description", description);
            if (!string.IsNullOrEmpty(category))
               IFCAnyHandleUtil.SetAttribute(materialLayer, "Category", category);
            if (priority.HasValue)
               IFCAnyHandleUtil.SetAttribute(materialLayer, "Priority", priority);
         }
         return materialLayer;
      }

      /// <summary>
      /// Creates an IfcMaterialLayerSet and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="materiallayers">The material layers.</param>
      /// <param name="name">The name.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateMaterialLayerSet(IFCFile file, IList<IFCAnyHandle> materiallayers, string name, string description=null)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(materiallayers, false, IFCEntityType.IfcMaterialLayer);

         IFCAnyHandle materialLayerSet = CreateInstance(file, IFCEntityType.IfcMaterialLayerSet, null);
         IFCAnyHandleUtil.SetAttribute(materialLayerSet, "MaterialLayers", materiallayers);
         IFCAnyHandleUtil.SetAttribute(materialLayerSet, "LayerSetName", name);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            if (!string.IsNullOrEmpty(description))
               IFCAnyHandleUtil.SetAttribute(materialLayerSet, "Description", description);
         }
         return materialLayerSet;
      }

      /// <summary>
      /// Creates an IfcMaterialLayerSetUsage and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="materialLayerSet">The material layer set handle.</param>
      /// <param name="direction">The direction of the material layer set.</param>
      /// <param name="directionSense">The direction sense.</param>
      /// <param name="offset">Offset of the material layer set base line (MlsBase) from reference geometry (line or plane).</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateMaterialLayerSetUsage(IFCFile file, IFCAnyHandle materialLayerSet, IFCLayerSetDirection direction,
          IFCDirectionSense directionSense, double offset)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(materialLayerSet, false, IFCEntityType.IfcMaterialLayerSet);

         IFCAnyHandle materialLayerSetUsage = CreateInstance(file, IFCEntityType.IfcMaterialLayerSetUsage, null);
         IFCAnyHandleUtil.SetAttribute(materialLayerSetUsage, "ForLayerSet", materialLayerSet);
         IFCAnyHandleUtil.SetAttribute(materialLayerSetUsage, "LayerSetDirection", direction);
         IFCAnyHandleUtil.SetAttribute(materialLayerSetUsage, "DirectionSense", directionSense);
         IFCAnyHandleUtil.SetAttribute(materialLayerSetUsage, "OffsetFromReferenceLine", offset);
         return materialLayerSetUsage;
      }

      /// <summary>
      /// Create IfcMaterialProfile and assign it to the file
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="Profile">Profile of the element that has a material assigned to it</param>
      /// <param name="name">name</param>
      /// <param name="description">description</param>
      /// <param name="Material">the Material of the Profile</param>
      /// <param name="priority">properity</param>
      /// <param name="category">category</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateMaterialProfile(IFCFile file, IFCAnyHandle Profile, string name = null, string description = null,
         IFCAnyHandle Material = null, double? priority = null, string category = null)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(Profile, false, IFCEntityType.IfcProfileDef);
         IFCAnyHandleUtil.ValidateSubTypeOf(Material, true, IFCEntityType.IfcMaterial);

         IFCAnyHandle materialProfile = CreateInstance(file, IFCEntityType.IfcMaterialProfile, null);
         IFCAnyHandleUtil.SetAttribute(materialProfile, "Profile", Profile);

         if (name != null)
            IFCAnyHandleUtil.SetAttribute(materialProfile, "Name", name);
         if (description != null)
            IFCAnyHandleUtil.SetAttribute(materialProfile, "Description", description);
         if (Material != null)
            IFCAnyHandleUtil.SetAttribute(materialProfile, "Material", Material);
         if (priority.HasValue)
            IFCAnyHandleUtil.SetAttribute(materialProfile, "Priority", priority.Value);
         if (category != null)
            IFCAnyHandleUtil.SetAttribute(materialProfile, "Category", category);
         return materialProfile;
      }

      /// <summary>
      /// Create IfcMaterialProfileSet and assign it to the file
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="materialprofiles">the list of IfcMaterialProfile</param>
      /// <param name="name">name</param>
      /// <param name="description">description</param>
      /// <param name="compositeProfile">Composite profile which this material profile set is associated to</param>
      /// <returns></returns>
      public static IFCAnyHandle CreateMaterialProfileSet(IFCFile file, IList<IFCAnyHandle> materialprofiles, string name = null, string description = null,
         IFCAnyHandle compositeProfile = null)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(materialprofiles, false, IFCEntityType.IfcMaterialProfile);
         IFCAnyHandleUtil.ValidateSubTypeOf(compositeProfile, true, IFCEntityType.IfcCompositeProfileDef);

         IFCAnyHandle materialProfileSet = CreateInstance(file, IFCEntityType.IfcMaterialProfileSet, null);
         IFCAnyHandleUtil.SetAttribute(materialProfileSet, "MaterialProfiles", materialprofiles);
         if (name != null)
            IFCAnyHandleUtil.SetAttribute(materialProfileSet, "Name", name);
         if (description != null)
            IFCAnyHandleUtil.SetAttribute(materialProfileSet, "Description", description);
         if (compositeProfile != null)
            IFCAnyHandleUtil.SetAttribute(materialProfileSet, "CompositeProfile", compositeProfile);
         return materialProfileSet;
      }

      /// <summary>
      /// Create IfcMaterialProfileSetUsage and assign it to the file
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="profileSet">the ProfileSet</param>
      /// <param name="cardinalPoint">cardinal point</param>
      /// <param name="referenceExtent">reference extent</param>
      /// <returns></returns>
      public static IFCAnyHandle CreateMaterialProfileSetUsage(IFCFile file, IFCAnyHandle profileSet, int? cardinalPoint, double? referenceExtent)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(profileSet, false, IFCEntityType.IfcMaterialProfileSet);

         IFCAnyHandle materialProfileSetUsage = CreateInstance(file, IFCEntityType.IfcMaterialProfileSetUsage, null);
         IFCAnyHandleUtil.SetAttribute(materialProfileSetUsage, "ForProfileSet", profileSet);
         if (cardinalPoint.HasValue)
            IFCAnyHandleUtil.SetAttribute(materialProfileSetUsage, "CardinalPoint", cardinalPoint);
         if (referenceExtent.HasValue)
            IFCAnyHandleUtil.SetAttribute(materialProfileSetUsage, "ReferenceExtent", referenceExtent.Value);
         return materialProfileSetUsage;
      }

      /// <summary>
      /// Create IfcMaterialProfileSetUsageTapering and assign it to the file
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="profileSet">profileSet</param>
      /// <param name="cardinalPoint">the cardinal point</param>
      /// <param name="referenceExtent">reference extent</param>
      /// <param name="forProfileEndSet">profileSet at the end</param>
      /// <param name="cardinalEndPoint">the cardinal point at the end</param>
      /// <returns></returns>
      public static IFCAnyHandle CreateMaterialProfileSetUsageTapering(IFCFile file, IFCAnyHandle profileSet, int? cardinalPoint, double? referenceExtent,
          IFCAnyHandle forProfileEndSet, int? cardinalEndPoint)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(profileSet, false, IFCEntityType.IfcMaterialProfileSet);
         IFCAnyHandleUtil.ValidateSubTypeOf(forProfileEndSet, false, IFCEntityType.IfcMaterialProfileSet);

         IFCAnyHandle materialProfileSetUsageTapering = CreateInstance(file, IFCEntityType.IfcMaterialProfileSetUsageTapering, null);
         IFCAnyHandleUtil.SetAttribute(materialProfileSetUsageTapering, "ForProfileSet", profileSet);
         IFCAnyHandleUtil.SetAttribute(materialProfileSetUsageTapering, "ForProfileEndSet", forProfileEndSet);

         if (cardinalPoint.HasValue)
            IFCAnyHandleUtil.SetAttribute(materialProfileSetUsageTapering, "CardinalPoint", cardinalPoint.Value);
         if (referenceExtent.HasValue)
            IFCAnyHandleUtil.SetAttribute(materialProfileSetUsageTapering, "ReferenceExtent", referenceExtent.Value);
         if (cardinalEndPoint.HasValue)
            IFCAnyHandleUtil.SetAttribute(materialProfileSetUsageTapering, "CardinalEndPoint", cardinalEndPoint.Value);
         return materialProfileSetUsageTapering;
      }

      public static IFCAnyHandle CreateMaterialConstituent(IFCFile file, IFCAnyHandle material, string name = null, string description = null,
           double? fraction = null, string category = null)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(material, true, IFCEntityType.IfcMaterial);

         IFCAnyHandle materialConstituent = CreateInstance(file, IFCEntityType.IfcMaterialConstituent, null);
         IFCAnyHandleUtil.SetAttribute(materialConstituent, "Material", material);

         if (name != null)
            IFCAnyHandleUtil.SetAttribute(materialConstituent, "Name", name);
         if (description != null)
            IFCAnyHandleUtil.SetAttribute(materialConstituent, "Description", description);
         if (fraction.HasValue)
            IFCAnyHandleUtil.SetAttribute(materialConstituent, "Fraction", fraction);
         if (category != null)
            IFCAnyHandleUtil.SetAttribute(materialConstituent, "Category", category);
         return materialConstituent;
      }

      /// <summary>
      /// Create IfcMaterialConstituentSet and assign it to the file
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="materialprofiles">the list of IfcMaterialProfile</param>
      /// <param name="name">name</param>
      /// <param name="description">description</param>
      /// <param name="compositeProfile">Composite profile which this material profile set is associated to</param>
      /// <returns></returns>
      public static IFCAnyHandle CreateMaterialConstituentSet(IFCFile file, HashSet<IFCAnyHandle> materialConstituents,
          string name = null, string description = null)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(materialConstituents, false, IFCEntityType.IfcMaterialConstituent);

         IFCAnyHandle materialConstituentSet = CreateInstance(file, IFCEntityType.IfcMaterialConstituentSet, null);
         IFCAnyHandleUtil.SetAttribute(materialConstituentSet, "MaterialConstituents", materialConstituents);
         if (name != null)
            IFCAnyHandleUtil.SetAttribute(materialConstituentSet, "Name", name);
         if (description != null)
            IFCAnyHandleUtil.SetAttribute(materialConstituentSet, "Description", description);

         return materialConstituentSet;
      }

      /// <summary>
      /// Creates an IfcOpeningElement, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The geometric representation of the entity.</param>
      /// <param name="allowTag">If false, don't create a tag for this opening element.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateOpeningElement(ExporterIFC exporterIFC, Element element, string guid,
          IFCAnyHandle ownerHistory, IFCAnyHandle objectPlacement, IFCAnyHandle representation, bool allowTag = true)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle openingElement = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcOpeningElement, element);
         SetElement(exporterIFC, openingElement, element, guid, ownerHistory, objectPlacement, representation, allowTag: allowTag);
         
         // In IFC4, Recess or Opening can be set in PreDefinedType attribute 
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            if (string.Compare(IFCAnyHandleUtil.GetStringAttribute(openingElement, "ObjectType"), "Recess", true) == 0)
               IFCAnyHandleUtil.SetAttribute(openingElement, "PreDefinedType", IFC4.IFCOpeningElementType.RECESS);
            else
               IFCAnyHandleUtil.SetAttribute(openingElement, "PreDefinedType", IFC4.IFCOpeningElementType.OPENING);
         }

         return openingElement;
      }

      /// <summary>
      /// Creates an IfcPlanarExtent and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="sizeInX">The extent in the direction of the x-axis.</param>
      /// <param name="sizeInY">The extent in the direction of the y-axis.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePlanarExtent(IFCFile file, double sizeInX, double sizeInY)
      {
         IFCAnyHandle planarExtent = CreateInstance(file, IFCEntityType.IfcPlanarExtent, null);
         IFCAnyHandleUtil.SetAttribute(planarExtent, "SizeInX", sizeInX);
         IFCAnyHandleUtil.SetAttribute(planarExtent, "SizeInY", sizeInY);
         return planarExtent;
      }

      /// <summary>
      /// Creates an IfcPresentationLayerAssignment and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="styles">A set of presentation styles that are assigned to styled items.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePresentationLayerAssignment(IFCFile file, string name, string description,
          ISet<IFCAnyHandle> assignedItems, string identifier)
      {
         ValidatePresentationLayerAssignment(name, assignedItems);

         IFCAnyHandle presentationLayerAssignment = CreateInstance(file, IFCEntityType.IfcPresentationLayerAssignment, null);
         SetPresentationLayerAssigment(presentationLayerAssignment, name, description, assignedItems, identifier);
         return presentationLayerAssignment;
      }

      /// <summary>
      /// Creates an IfcPresentationStyleAssignment and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="styles">A set of presentation styles that are assigned to styled items.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePresentationStyleAssignment(IFCFile file, ISet<IFCAnyHandle> styles)
      {
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            // IfcSymbolSytle has been deleted in IFC4
            IFCAnyHandleUtil.ValidateSubTypeOf(styles, false, IFCEntityType.IfcCurveStyle,
                IFCEntityType.IfcFillAreaStyle, IFCEntityType.IfcTextStyle, IFCEntityType.IfcSurfaceStyle);
         }
         else
         {
            IFCAnyHandleUtil.ValidateSubTypeOf(styles, false, IFCEntityType.IfcCurveStyle, IFCEntityType.IfcSymbolStyle,
                IFCEntityType.IfcFillAreaStyle, IFCEntityType.IfcTextStyle, IFCEntityType.IfcSurfaceStyle);
         }
         IFCAnyHandle presentationStyleAssignment = CreateInstance(file, IFCEntityType.IfcPresentationStyleAssignment, null);
         IFCAnyHandleUtil.SetAttribute(presentationStyleAssignment, "Styles", styles);
         return presentationStyleAssignment;
      }

      /// <summary>
      /// Creates an IfcQuantityArea and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="unit">The unit.</param>
      /// <param name="areaValue">The value of the quantity, in the appropriate units.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateQuantityArea(IFCFile file, string name, string description, IFCAnyHandle unit, double areaValue)
      {
         ValidatePhysicalSimpleQuantity(name, description, unit);

         IFCAnyHandle quantityArea = CreateInstance(file, IFCEntityType.IfcQuantityArea, null);
         IFCAnyHandleUtil.SetAttribute(quantityArea, "AreaValue", areaValue);
         SetPhysicalSimpleQuantity(quantityArea, name, description, unit);
         return quantityArea;
      }

      /// <summary>
      /// Creates an IfcQuantityLength and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="unit">The unit.</param>
      /// <param name="lengthValue">The value of the quantity, in the appropriate units.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateQuantityLength(IFCFile file, string name, string description, IFCAnyHandle unit, double lengthValue)
      {
         ValidatePhysicalSimpleQuantity(name, description, unit);

         IFCAnyHandle quantityLength = CreateInstance(file, IFCEntityType.IfcQuantityLength, null);
         IFCAnyHandleUtil.SetAttribute(quantityLength, "LengthValue", lengthValue);
         SetPhysicalSimpleQuantity(quantityLength, name, description, unit);
         return quantityLength;
      }

      /// <summary>
      /// Creates an IfcQuantityVolume and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="unit">The unit.</param>
      /// <param name="lengthValue">The value of the quantity, in the appropriate units.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateQuantityVolume(IFCFile file, string name, string description, IFCAnyHandle unit, double volumeValue)
      {
         ValidatePhysicalSimpleQuantity(name, description, unit);

         IFCAnyHandle quantityVolume = CreateInstance(file, IFCEntityType.IfcQuantityVolume, null);
         IFCAnyHandleUtil.SetAttribute(quantityVolume, "VolumeValue", volumeValue);
         SetPhysicalSimpleQuantity(quantityVolume, name, description, unit);
         return quantityVolume;
      }

      public static IFCAnyHandle CreateQuantityWeight(IFCFile file, string name, string description, IFCAnyHandle unit, double weightValue)
      {
         ValidatePhysicalSimpleQuantity(name, description, unit);

         IFCAnyHandle quantityWeight = CreateInstance(file, IFCEntityType.IfcQuantityWeight, null);
         IFCAnyHandleUtil.SetAttribute(quantityWeight, "WeightValue", weightValue);
         SetPhysicalSimpleQuantity(quantityWeight, name, description, unit);
         return quantityWeight;
      }

      /// <summary>
      /// Creates an IfcRelConnectsPorts and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatingPort">The port handle.</param>
      /// <param name="relatedPort">The port handle.</param>
      /// <param name="realizingElement">The element handle.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRelConnectsPorts(IFCFile file, string guid, IFCAnyHandle ownerHistory, string name, string description,
          IFCAnyHandle relatingPort, IFCAnyHandle relatedPort, IFCAnyHandle realizingElement)
      {
         ValidateRelConnects(guid, ownerHistory, name);
         IFCAnyHandleUtil.ValidateSubTypeOf(relatingPort, false, IFCEntityType.IfcPort);
         IFCAnyHandleUtil.ValidateSubTypeOf(relatedPort, false, IFCEntityType.IfcPort);
         IFCAnyHandleUtil.ValidateSubTypeOf(realizingElement, true, IFCEntityType.IfcElement);

         IFCAnyHandle relConnectsPorts = CreateInstance(file, IFCEntityType.IfcRelConnectsPorts, null);
         IFCAnyHandleUtil.SetAttribute(relConnectsPorts, "RelatingPort", relatingPort);
         IFCAnyHandleUtil.SetAttribute(relConnectsPorts, "RelatedPort", relatedPort);
         IFCAnyHandleUtil.SetAttribute(relConnectsPorts, "RealizingElement", realizingElement);
         SetRelConnects(relConnectsPorts, guid, ownerHistory, name, description);
         return relConnectsPorts;
      }

      /// <summary>
      /// Creates an IfcRelServicesBuildings and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatingSystem">The system handle.</param>
      /// <param name="relatedBuildings">The related spatial structure handles.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRelServicesBuildings(IFCFile file, string guid, IFCAnyHandle ownerHistory, string name, string description,
          IFCAnyHandle relatingSystem, ISet<IFCAnyHandle> relatedBuildings)
      {
         ValidateRelConnects(guid, ownerHistory, name);
         IFCAnyHandleUtil.ValidateSubTypeOf(relatingSystem, false, IFCEntityType.IfcSystem);
         IFCAnyHandleUtil.ValidateSubTypeOf(relatedBuildings, false, IFCEntityType.IfcSpatialStructureElement);

         IFCAnyHandle relServicesBuildings = CreateInstance(file, IFCEntityType.IfcRelServicesBuildings, null);
         IFCAnyHandleUtil.SetAttribute(relServicesBuildings, "RelatingSystem", relatingSystem);
         IFCAnyHandleUtil.SetAttribute(relServicesBuildings, "RelatedBuildings", relatedBuildings);
         SetRelConnects(relServicesBuildings, guid, ownerHistory, name, description);
         return relServicesBuildings;
      }

      /// <summary>
      /// Creates an IfcRelConnectsPortToElement and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatingPort">The port handle.</param>
      /// <param name="relatedElement">The element handle.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRelConnectsPortToElement(IFCFile file, string guid, IFCAnyHandle ownerHistory, string name, string description,
          IFCAnyHandle relatingPort, IFCAnyHandle relatedElement)
      {
         ValidateRelConnects(guid, ownerHistory, name);
         IFCAnyHandleUtil.ValidateSubTypeOf(relatingPort, false, IFCEntityType.IfcPort);
         IFCAnyHandleUtil.ValidateSubTypeOf(relatedElement, false, IFCEntityType.IfcElement);

         IFCAnyHandle relConnectsPortToElement = CreateInstance(file, IFCEntityType.IfcRelConnectsPortToElement, null);
         IFCAnyHandleUtil.SetAttribute(relConnectsPortToElement, "RelatingPort", relatingPort);
         IFCAnyHandleUtil.SetAttribute(relConnectsPortToElement, "RelatedElement", relatedElement);
         SetRelConnects(relConnectsPortToElement, guid, ownerHistory, name, description);
         return relConnectsPortToElement;
      }

      /// <summary>
      /// Creates an IfcRelFillsElement, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatingOpeningElement">The opening element.</param>
      /// <param name="relatedBuildingElement">The building element that fills or partially fills the opening.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRelFillsElement(IFCFile file, string guid, IFCAnyHandle ownerHistory, string name, string description,
          IFCAnyHandle relatingOpeningElement, IFCAnyHandle relatedBuildingElement)
      {
         ValidateRelConnects(guid, ownerHistory, name);
         IFCAnyHandleUtil.ValidateSubTypeOf(relatingOpeningElement, false, IFCEntityType.IfcOpeningElement);
         IFCAnyHandleUtil.ValidateSubTypeOf(relatedBuildingElement, false, IFCEntityType.IfcElement);

         IFCAnyHandle relFillsElement = CreateInstance(file, IFCEntityType.IfcRelFillsElement, null);
         IFCAnyHandleUtil.SetAttribute(relFillsElement, "RelatingOpeningElement", relatingOpeningElement);
         IFCAnyHandleUtil.SetAttribute(relFillsElement, "RelatedBuildingElement", relatedBuildingElement);
         SetRelConnects(relFillsElement, guid, ownerHistory, name, description);
         return relFillsElement;
      }

      /// <summary>
      /// Creates an IfcRelSpaceBoundary and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatingSpace">The relating space handle.</param>
      /// <param name="relatedBuildingElement">The related building element.</param>
      /// <param name="connectionGeometry">The connection geometry.</param>
      /// <param name="physicalOrVirtual">The space boundary type, physical or virtual.</param>
      /// <param name="internalOrExternal">Internal or external.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRelSpaceBoundary(IFCFile file, string guid, IFCAnyHandle ownerHistory, string name, string description,
          IFCAnyHandle relatingSpace, IFCAnyHandle relatedBuildingElement, IFCAnyHandle connectionGeometry, IFCPhysicalOrVirtual physicalOrVirtual,
          IFCInternalOrExternal internalOrExternal)
      {
         ValidateRelConnects(guid, ownerHistory, name);
         IFCAnyHandleUtil.ValidateSubTypeOf(relatingSpace, false, IFCEntityType.IfcSpace);
         IFCAnyHandleUtil.ValidateSubTypeOf(relatedBuildingElement, true, IFCEntityType.IfcElement);
         IFCAnyHandleUtil.ValidateSubTypeOf(connectionGeometry, true, IFCEntityType.IfcConnectionGeometry);

         IFCAnyHandle relSpaceBoundary = CreateInstance(file, IFCEntityType.IfcRelSpaceBoundary, null);
         IFCAnyHandleUtil.SetAttribute(relSpaceBoundary, "RelatingSpace", relatingSpace);
         IFCAnyHandleUtil.SetAttribute(relSpaceBoundary, "RelatedBuildingElement", relatedBuildingElement);
         IFCAnyHandleUtil.SetAttribute(relSpaceBoundary, "ConnectionGeometry", connectionGeometry);
         IFCAnyHandleUtil.SetAttribute(relSpaceBoundary, "PhysicalOrVirtualBoundary", physicalOrVirtual);
         IFCAnyHandleUtil.SetAttribute(relSpaceBoundary, "InternalOrExternalBoundary", internalOrExternal);
         SetRelConnects(relSpaceBoundary, guid, ownerHistory, name, description);
         return relSpaceBoundary;
      }

      /// <summary>
      /// Creates an IfcRelVoidsElement, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="relatingBuildingElement">The building element.</param>
      /// <param name="relatedOpeningElement">The opening element that removes material from the building element.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRelVoidsElement(IFCFile file, string guid, IFCAnyHandle ownerHistory, string name, string description,
          IFCAnyHandle relatingBuildingElement, IFCAnyHandle relatedOpeningElement)
      {
         ValidateRelConnects(guid, ownerHistory, name);
         IFCAnyHandleUtil.ValidateSubTypeOf(relatingBuildingElement, false, IFCEntityType.IfcElement);
         IFCAnyHandleUtil.ValidateSubTypeOf(relatedOpeningElement, false, IFCEntityType.IfcFeatureElementSubtraction);

         IFCAnyHandle relVoidsElement = CreateInstance(file, IFCEntityType.IfcRelVoidsElement, null);
         IFCAnyHandleUtil.SetAttribute(relVoidsElement, "RelatingBuildingElement", relatingBuildingElement);
         IFCAnyHandleUtil.SetAttribute(relVoidsElement, "RelatedOpeningElement", relatedOpeningElement);
         SetRelConnects(relVoidsElement, guid, ownerHistory, name, description);
         return relVoidsElement;
      }

      /// <summary>
      /// Creates an IfcShapeRepresentation and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="contextOfItems">The context of the items.</param>
      /// <param name="identifier">The identifier.</param>
      /// <param name="type">The representation type.</param>
      /// <param name="items">The items that belong to the shape representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateShapeRepresentation(IFCFile file,
          IFCAnyHandle contextOfItems, string identifier, string type, ISet<IFCAnyHandle> items)
      {
         ValidateRepresentation(contextOfItems, identifier, type, items);

         IFCAnyHandle shapeRepresentation = CreateInstance(file, IFCEntityType.IfcShapeRepresentation, null);
         SetRepresentation(shapeRepresentation, contextOfItems, identifier, type, items);
         return shapeRepresentation;
      }

      /// <summary>
      /// Creates an IfcSite and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The product representation.</param>
      /// <param name="longName">The long name.</param>
      /// <param name="compositionType">The composition type.</param>
      /// <param name="latitude">The latitude.</param>
      /// <param name="longitude">The longitude.</param>
      /// <param name="elevation">The elevation.</param>
      /// <param name="landTitleNumber">The title number.</param>
      /// <param name="address">The address.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSite(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory, string name,
          string description, IFCAnyHandle objectPlacement, IFCAnyHandle representation, string longName,
          IFCElementComposition compositionType, IList<int> latitude, IList<int> longitude,
          double? elevation, string landTitleNumber, IFCAnyHandle address)
      {
         ValidateSpatialStructureElement(guid, ownerHistory, objectPlacement, representation, compositionType);
         IFCAnyHandleUtil.ValidateSubTypeOf(address, true, IFCEntityType.IfcPostalAddress);

         IFCAnyHandle site = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcSite, element);
         IFCAnyHandleUtil.SetAttribute(site, "RefLatitude", latitude);
         IFCAnyHandleUtil.SetAttribute(site, "RefLongitude", longitude);
         IFCAnyHandleUtil.SetAttribute(site, "RefElevation", elevation);
         IFCAnyHandleUtil.SetAttribute(site, "LandTitleNumber", landTitleNumber);
         IFCAnyHandleUtil.SetAttribute(site, "SiteAddress", address);
         SetSpatialStructureElement(exporterIFC, site, element, guid, ownerHistory, name, description, objectPlacement, representation, longName, compositionType);
         return site;
      }

      /// <summary>
      /// Creates an IfcStyledItem and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="item">The geometric representation item to which the style is assigned.</param>
      /// <param name="styles">Representation style assignments which are assigned to an item.</param>
      /// <param name="name">The word, or group of words, by which the styled item is referred to.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateStyledItem(IFCFile file,
          IFCAnyHandle item, HashSet<IFCAnyHandle> styles, string name)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(item, true, IFCEntityType.IfcRepresentationItem);
         IFCAnyHandleUtil.ValidateSubTypeOf(styles, false, IFCEntityType.IfcPresentationStyleAssignment);

         IFCAnyHandle styledItem = CreateInstance(file, IFCEntityType.IfcStyledItem, null);
         IFCAnyHandleUtil.SetAttribute(styledItem, "Item", item);
         IFCAnyHandleUtil.SetAttribute(styledItem, "Styles", styles);
         IFCAnyHandleUtil.SetAttribute(styledItem, "Name", name);
         return styledItem;
      }

      /// <summary>
      /// Creates an IfcStyledRepresentation and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="representation">The IfcRepresentation.</param>
      /// <param name="contextOfItems">The context of the items.</param>
      /// <param name="identifier">The identifier.</param>
      /// <param name="type">The representation type.</param>
      /// <param name="items">The items that belong to the shape representation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateStyledRepresentation(IFCFile file, IFCAnyHandle contextOfItems, string identifier, string type,
          HashSet<IFCAnyHandle> items)
      {
         ValidateRepresentation(contextOfItems, identifier, type, items);

         IFCAnyHandle styledRepresentation = CreateInstance(file, IFCEntityType.IfcStyledRepresentation, null);
         SetRepresentation(styledRepresentation, contextOfItems, identifier, type, items);
         return styledRepresentation;
      }

      /// <summary>
      /// Creates an IfcTextLiteralWithExtent and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="literal">The text literal to be presented.</param>
      /// <param name="placement">The IfcAxis2Placement that determines the placement and orientation of the presented string.</param>
      /// <param name="path">The writing direction of the text literal.</param>
      /// <param name="extent">The extent in the x and y direction of the text literal.</param>
      /// <param name="boxAlignment">The alignment of the text literal relative to its position.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateTextLiteralWithExtent(IFCFile file,
          string literal, IFCAnyHandle placement, IFCTextPath path, IFCAnyHandle extent, string boxAlignment)
      {
         if (literal == null)
            throw new ArgumentNullException("literal");
         if (boxAlignment == null)
            throw new ArgumentNullException("boxAlignment");
         IFCAnyHandleUtil.ValidateSubTypeOf(placement, false, IFCEntityType.IfcAxis2Placement2D, IFCEntityType.IfcAxis2Placement3D);
         IFCAnyHandleUtil.ValidateSubTypeOf(extent, false, IFCEntityType.IfcPlanarExtent);

         IFCAnyHandle textLiteralWithExtent = CreateInstance(file, IFCEntityType.IfcTextLiteralWithExtent, null);
         IFCAnyHandleUtil.SetAttribute(textLiteralWithExtent, "Literal", literal);
         IFCAnyHandleUtil.SetAttribute(textLiteralWithExtent, "Placement", placement);
         IFCAnyHandleUtil.SetAttribute(textLiteralWithExtent, "Path", path);
         IFCAnyHandleUtil.SetAttribute(textLiteralWithExtent, "Extent", extent);
         IFCAnyHandleUtil.SetAttribute(textLiteralWithExtent, "BoxAlignment", boxAlignment);
         return textLiteralWithExtent;
      }

      /// <summary>
      /// Creates an IfcTextStyle and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="name">The name.</param>
      /// <param name="characterAppearance">The character style to be used for presented text.</param>
      /// <param name="style">The style applied to the text block for its visual appearance.</param>
      /// <param name="fontStyle">The style applied to the text font for its visual appearance.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateTextStyle(IFCFile file,
          string name, IFCAnyHandle characterAppearance, IFCAnyHandle style, IFCAnyHandle fontStyle)
      {
         ValidatePresentationStyle(name);
         IFCAnyHandleUtil.ValidateSubTypeOf(characterAppearance, true, IFCEntityType.IfcTextStyleForDefinedFont);
         IFCAnyHandleUtil.ValidateSubTypeOf(style, true, IFCEntityType.IfcTextStyleWithBoxCharacteristics, IFCEntityType.IfcTextStyleTextModel);
         IFCAnyHandleUtil.ValidateSubTypeOf(fontStyle, false, IFCEntityType.IfcPreDefinedTextFont, IFCEntityType.IfcExternallyDefinedTextFont);

         IFCAnyHandle textStyle = CreateInstance(file, IFCEntityType.IfcTextStyle, null);
         IFCAnyHandleUtil.SetAttribute(textStyle, "TextCharacterAppearance", characterAppearance);
         IFCAnyHandleUtil.SetAttribute(textStyle, "TextStyle", style);
         IFCAnyHandleUtil.SetAttribute(textStyle, "TextFontStyle", fontStyle);
         SetPresentationStyle(textStyle, name);
         return textStyle;
      }

      /// <summary>
      /// Creates an IfcTextStyleFontModel and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="name">The name.</param>
      /// <param name="fontFamily">The font family.</param>
      /// <param name="fontStyle">The font style.</param>
      /// <param name="fontVariant">The font variant.</param>
      /// <param name="fontWeight">The font weight.</param>
      /// <param name="fontSize">The font size.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateTextStyleFontModel(IFCFile file,
          string name, IList<string> fontFamily, string fontStyle, string fontVariant,
          string fontWeight, IFCData fontSize)
      {
         ValidatePreDefinedItem(name);
         if (fontSize == null)
            throw new ArgumentNullException("fontSize");

         IFCAnyHandle textStyleFontModel = CreateInstance(file, IFCEntityType.IfcTextStyleFontModel, null);
         IFCAnyHandleUtil.SetAttribute(textStyleFontModel, "FontFamily", fontFamily);
         IFCAnyHandleUtil.SetAttribute(textStyleFontModel, "FontStyle", fontStyle);
         IFCAnyHandleUtil.SetAttribute(textStyleFontModel, "FontVariant", fontVariant);
         IFCAnyHandleUtil.SetAttribute(textStyleFontModel, "FontWeight", fontWeight);
         IFCAnyHandleUtil.SetAttribute(textStyleFontModel, "FontSize", fontSize);
         SetPreDefinedItem(textStyleFontModel, name);
         return textStyleFontModel;
      }

      /// <summary>
      /// Creates an IfcTextStyleForDefinedFont and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="color">The color.</param>
      /// <param name="backgroundColor">The background color.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateTextStyleForDefinedFont(IFCFile file,
          IFCAnyHandle color, IFCAnyHandle backgroundColor)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(color, false, IFCEntityType.IfcColourSpecification, IFCEntityType.IfcPreDefinedColour);
         IFCAnyHandleUtil.ValidateSubTypeOf(backgroundColor, true, IFCEntityType.IfcColourSpecification, IFCEntityType.IfcPreDefinedColour);

         IFCAnyHandle textStyleForDefinedFont = CreateInstance(file, IFCEntityType.IfcTextStyleForDefinedFont, null);
         IFCAnyHandleUtil.SetAttribute(textStyleForDefinedFont, "Colour", color);
         IFCAnyHandleUtil.SetAttribute(textStyleForDefinedFont, "BackgroundColour", backgroundColor);
         return textStyleForDefinedFont;
      }

      /// <summary>
      /// Creates an IfcTransportElement, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID to use to label the wall.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The representation object assigned to the wall.</param>
      /// <param name="elementTag">The tag for the identifier of the element.</param>
      /// <param name="operationType">The transport operation type. | in IFC4 this attribute becomes PreDefinedType</param>
      /// <param name="capacityByWeight">The capacity by weight.</param>
      /// <param name="capacityByNumber">The capacity by number.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateTransportElement(ExporterIFC exporterIFC, Element element,
          string guid, IFCAnyHandle ownerHistory, IFCAnyHandle objectPlacement, IFCAnyHandle representation,
          string operationType, double? capacityByWeight, double? capacityByNumber)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle transportElement = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcTransportElement, element);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            IFCAnyHandleUtil.SetAttribute(transportElement, "PreDefinedType", operationType, true);
         }
         else
         {
            IFCAnyHandleUtil.SetAttribute(transportElement, "OperationType", operationType, true);
            IFCAnyHandleUtil.SetAttribute(transportElement, "CapacityByWeight", capacityByWeight);
            IFCAnyHandleUtil.SetAttribute(transportElement, "CapacityByNumber", capacityByNumber);
         }
         SetElement(exporterIFC, transportElement, element, guid, ownerHistory, objectPlacement, representation);
         return transportElement;
      }

      /// <summary>
      /// Creates IfcIndexedPolygonalFace and assigns it to the file
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="coordIndex">the coordIndex</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateIndexedPolygonalFace(IFCFile file, IList<int> coordIndex)
      {
         if (coordIndex == null)
            throw new ArgumentNullException("CoordIndex");
         if (coordIndex.Count < 3)
            throw new IndexOutOfRangeException("CoordIndex must be at least 3 members");

         IFCAnyHandle indexedPolygonalFace = CreateInstance(file, IFCEntityType.IfcIndexedPolygonalFace, null);
         IFCAnyHandleUtil.SetAttribute(indexedPolygonalFace, "CoordIndex", coordIndex);

         return indexedPolygonalFace;
      }

      /// <summary>
      /// Creates IfcIndexedPolygonalFaceWithVoids and assigns it to the file
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="coordIndex">the CoordIndex</param>
      /// <param name="innerCoordIndices">the hole/void coordinates indices</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateIndexedPolygonalFaceWithVoids(IFCFile file, IList<int> coordIndex, IList<IList<int>> innerCoordIndices)
      {
         if (coordIndex == null)
            throw new ArgumentNullException("CoordIndex");
         if (coordIndex == null)
            throw new ArgumentNullException("InnerCoordIndices");
         if (coordIndex.Count < 3)
            throw new IndexOutOfRangeException("CoordIndex must be at least 3 members");
         ValidateListOfList(innerCoordIndices, false, "InnerCoordIndices");

         IFCAnyHandle indexedPolygonalFaceWithVoids = CreateInstance(file, IFCEntityType.IfcIndexedPolygonalFaceWithVoids, null);
         IFCAnyHandleUtil.SetAttribute(indexedPolygonalFaceWithVoids, "CoordIndex", coordIndex);
         IFCAnyHandleUtil.SetAttribute(indexedPolygonalFaceWithVoids, "InnerCoordIndices", innerCoordIndices, 1, null, 3, null);

         return indexedPolygonalFaceWithVoids;
      }

      /// <summary>
      /// Creates the IfcPolygonalFaceSet and assigns it to the file
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="coordinates">coordinates list</param>
      /// <param name="closed">closed or open faceSet</param>
      /// <param name="faces">the Faces</param>
      /// <param name="pnIndex">the Optional point index</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreatePolygonalFaceSet(IFCFile file, IFCAnyHandle coordinates, bool? closed, IList<IFCAnyHandle> faces, IList<int> pnIndex)
      {
         if (coordinates == null)
            throw new ArgumentNullException("coordinates");
         IFCAnyHandleUtil.ValidateSubTypeOf(coordinates, false, IFCEntityType.IfcCartesianPointList3D);
         IFCAnyHandleUtil.ValidateSubTypeOf(faces[0], false, IFCEntityType.IfcIndexedPolygonalFace);

         IFCAnyHandle polygonalFaceSet = CreateInstance(file, IFCEntityType.IfcPolygonalFaceSet, null);
         IFCAnyHandleUtil.SetAttribute(polygonalFaceSet, "Coordinates", coordinates);
         IFCAnyHandleUtil.SetAttribute(polygonalFaceSet, "Closed", closed);
         IFCAnyHandleUtil.SetAttribute(polygonalFaceSet, "Faces", faces);
         IFCAnyHandleUtil.SetAttribute(polygonalFaceSet, "PnIndex", pnIndex);

         return polygonalFaceSet;
      }

      /// <summary>
      /// Create an instance of IFCTriangulatedFaceSet
      /// </summary>
      /// <param name="file">the IFC file</param>
      /// <param name="coordinates">Coordinates attribute (IfcCartesianPointList3D)</param>
      /// <param name="normals">List of Normals</param>
      /// <param name="closed">Closed attribute</param>
      /// <param name="coordIndex">Triangle Indexes</param>
      /// <param name="normalIndex">Normal Indexes</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateTriangulatedFaceSet(IFCFile file, IFCAnyHandle coordinates, IList<IList<double>> normals, bool? closed,
              IList<IList<int>> coordIndex, IList<IList<int>> normalIndex = null, IList<int> pnIndex = null)
      {
         if (coordinates == null)
            throw new ArgumentNullException("coordinates");
         IFCAnyHandleUtil.ValidateSubTypeOf(coordinates, false, IFCEntityType.IfcCartesianPointList3D);
         ValidateListOfList(normals, true, "Normals");
         ValidateListOfList(coordIndex, false, "CoordIndex");
         ValidateListOfList(normalIndex, true, "NormalIndex");

         IFCAnyHandle triangulatedFaceSet = CreateInstance(file, IFCEntityType.IfcTriangulatedFaceSet, null);
         IFCAnyHandleUtil.SetAttribute(triangulatedFaceSet, "Coordinates", coordinates);
         IFCAnyHandleUtil.SetAttribute(triangulatedFaceSet, "Normals", normals, 1, null, 3, 3);
         IFCAnyHandleUtil.SetAttribute(triangulatedFaceSet, "Closed", closed);
         IFCAnyHandleUtil.SetAttribute(triangulatedFaceSet, "CoordIndex", coordIndex, 1, null, 3, 3);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            IFCAnyHandleUtil.SetAttribute(triangulatedFaceSet, "PnIndex", pnIndex);
         else
            IFCAnyHandleUtil.SetAttribute(triangulatedFaceSet, "NormalIndex", normalIndex, 1, null, 3, 3);

         return triangulatedFaceSet;
      }

      /// <summary>
      /// Creates an IfcWindow, and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID to use to label the wall.</param>
      /// <param name="ownerHistory">The IfcOwnerHistory.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The local placement.</param>
      /// <param name="representation">The representation object assigned to the wall.</param>
      /// <param name="elementTag">The tag for the identifier of the element.</param>
      /// <param name="height">The height of the window.</param>
      /// <param name="width">The width of the window.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateWindow(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation,
          double? height, double? width, string preDefinedType, string partitioningType, string userDefinedPartitioningType)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle window = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcWindow, element);
         IFCAnyHandleUtil.SetAttribute(window, "OverallHeight", height);
         IFCAnyHandleUtil.SetAttribute(window, "OverallWidth", width);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            string validatedType = IFCValidateEntry.ValidateStrEnum<IFC4.IFCWindowType>(preDefinedType);
            IFCAnyHandleUtil.SetAttribute(window, "PreDefinedType", validatedType, true);
            validatedType = IFCValidateEntry.ValidateStrEnum<IFC4.IFCWindowTypePartitioning>(partitioningType);
            IFCAnyHandleUtil.SetAttribute(window, "PartitioningType", validatedType, true);
            if (String.Compare(partitioningType, "UserDefined", true) == 0 && string.IsNullOrEmpty(userDefinedPartitioningType))
               IFCAnyHandleUtil.SetAttribute(window, "UserDefinedPartitioningType", userDefinedPartitioningType);
         }
         SetElement(exporterIFC, window, element, guid, ownerHistory, objectPlacement, representation);
         return window;
      }

      /// <summary>
      /// Creates an IfcPropertySingleValue and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="nominalValue">The value of the property.</param>
      /// <param name="unit">The unit.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePropertySingleValue(IFCFile file,
          string name, string description, IFCData nominalValue, IFCAnyHandle unit)
      {
         ValidateProperty(name, description);
         IFCAnyHandleUtil.ValidateSubTypeOf(unit, true, IFCEntityType.IfcDerivedUnit, IFCEntityType.IfcNamedUnit, IFCEntityType.IfcMonetaryUnit);

         IFCAnyHandle propertySingleValue = CreateInstance(file, IFCEntityType.IfcPropertySingleValue, null);
         IFCAnyHandleUtil.SetAttribute(propertySingleValue, "NominalValue", nominalValue);
         IFCAnyHandleUtil.SetAttribute(propertySingleValue, "Unit", unit);
         SetProperty(propertySingleValue, name, description);
         return propertySingleValue;
      }

      /// <summary>
      /// Creates an IfcPropertyEnumeratedValue and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="enumerationValues">The values of the property.</param>
      /// <param name="enumerationReference">The enumeration reference.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePropertyEnumeratedValue(IFCFile file,
          string name, string description, IList<IFCData> enumerationValues, IFCAnyHandle enumerationReference)
      {
         ValidateProperty(name, description);
         IFCAnyHandleUtil.ValidateSubTypeOf(enumerationReference, true, IFCEntityType.IfcPropertyEnumeration);

         IFCAnyHandle propertyEnumeratedValue = CreateInstance(file, IFCEntityType.IfcPropertyEnumeratedValue, null);
         IFCAnyHandleUtil.SetAttribute(propertyEnumeratedValue, "EnumerationValues", enumerationValues);
         IFCAnyHandleUtil.SetAttribute(propertyEnumeratedValue, "EnumerationReference", enumerationReference);
         SetProperty(propertyEnumeratedValue, name, description);
         return propertyEnumeratedValue;
      }

      /// <summary>
      /// Creates an IfcPropertyReferenceValue and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="usageName">The use of the value within the property.</param>
      /// <param name="propertyReference">The entity being referenced.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePropertyReferenceValue(IFCFile file,
          string name, string description, string usageName, IFCAnyHandle propertyReference)
      {
         ValidateProperty(name, description);
         IFCAnyHandleUtil.ValidateSubTypeOf(propertyReference, false,
             IFCEntityType.IfcMaterial, IFCEntityType.IfcPerson, IFCEntityType.IfcDateAndTime, IFCEntityType.IfcMaterialList,
             IFCEntityType.IfcOrganization, IFCEntityType.IfcCalendarDate, IFCEntityType.IfcLocalTime, IFCEntityType.IfcPersonAndOrganization,
             IFCEntityType.IfcMaterialLayer, IFCEntityType.IfcExternalReference, IFCEntityType.IfcTimeSeries, IFCEntityType.IfcAddress,
             IFCEntityType.IfcAppliedValue);

         IFCAnyHandle propertyReferenceValue = CreateInstance(file, IFCEntityType.IfcPropertyReferenceValue, null);
         IFCAnyHandleUtil.SetAttribute(propertyReferenceValue, "UsageName", usageName);
         IFCAnyHandleUtil.SetAttribute(propertyReferenceValue, "PropertyReference", propertyReference);
         SetProperty(propertyReferenceValue, name, description);
         return propertyReferenceValue;
      }

      /// <summary>
      /// Creates an IfcPropertyListValue and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="listValues">The values of the property.</param>
      /// <param name="unit">The unit.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreatePropertyListValue(IFCFile file,
          string name, string description, IList<IFCData> listValues, IFCAnyHandle unit)
      {
         ValidateProperty(name, description);
         IFCAnyHandleUtil.ValidateSubTypeOf(unit, true, IFCEntityType.IfcDerivedUnit, IFCEntityType.IfcNamedUnit, IFCEntityType.IfcMonetaryUnit);

         IFCAnyHandle propertyListValue = CreateInstance(file, IFCEntityType.IfcPropertyListValue, null);
         IFCAnyHandleUtil.SetAttribute(propertyListValue, "ListValues", listValues);
         IFCAnyHandleUtil.SetAttribute(propertyListValue, "Unit", unit);
         SetProperty(propertyListValue, name, description);
         return propertyListValue;
      }

      /// <summary>
      /// Creates a handle representing an IfcCalendarDate and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="day">The day of the month in the date.</param>
      /// <param name="month">The month in the date.</param>
      /// <param name="year">The year in the date.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateCalendarDate(IFCFile file, int day, int month, int year)
      {
         IFCAnyHandle calendarDate = CreateInstance(file, IFCEntityType.IfcCalendarDate, null);
         IFCAnyHandleUtil.SetAttribute(calendarDate, "DayComponent", day);
         IFCAnyHandleUtil.SetAttribute(calendarDate, "MonthComponent", month);
         IFCAnyHandleUtil.SetAttribute(calendarDate, "YearComponent", year);
         return calendarDate;
      }

      /// <summary>
      /// Creates a handle representing an IfcClassification and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="source">The source of the classification.</param>
      /// <param name="edition">The edition of the classification system.</param>
      /// <param name="editionDate">The date associated with this edition of the classification system.</param>
      /// <param name="name">The name of the classification.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateClassification(IFCFile file, string source, string edition, IFCAnyHandle editionDate,
         string name)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(editionDate, true, IFCEntityType.IfcCalendarDate);

         IFCAnyHandle classification = CreateInstance(file, IFCEntityType.IfcClassification, null);
         IFCAnyHandleUtil.SetAttribute(classification, "Source", source);
         IFCAnyHandleUtil.SetAttribute(classification, "Edition", edition);
         IFCAnyHandleUtil.SetAttribute(classification, "EditionDate", editionDate);
         IFCAnyHandleUtil.SetAttribute(classification, "Name", name);
         return classification;
      }

      /// <summary>
      /// Creates a handle representing an IfcClassificationReference and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="location">Location of the reference (e.g. URL).</param>
      /// <param name="itemReference">Location of the item within the reference source.</param>
      /// <param name="name">Name of the reference.</param>
      /// <param name="referencedSource">The referenced classification.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateClassificationReference(IFCFile file, string location,
         string itemReference, string name, IFCAnyHandle referencedSource)
      {
         // All IfcExternalReference arguments are optional.
         IFCAnyHandleUtil.ValidateSubTypeOf(referencedSource, true, IFCEntityType.IfcClassification);

         IFCAnyHandle classificationReference = CreateInstance(file, IFCEntityType.IfcClassificationReference, null);
         SetExternalReference(classificationReference, location, itemReference, name);
         IFCAnyHandleUtil.SetAttribute(classificationReference, "ReferencedSource", referencedSource);
         return classificationReference;
      }

      /// <summary>
      /// Creates a handle representing an IfcRelAssociatesClassification and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="globalId">The GUID of the IfcRelAssociatesClassification.</param>
      /// <param name="ownerHistory">The owner history of the IfcRelAssociatesClassification.</param>
      /// <param name="name">Name of the relation.</param>
      /// <param name="description">Description of the relation.</param>
      /// <param name="relatedObjects">The handles of the objects associated to the classification.</param>
      /// <param name="relatingClassification">The classification assigned to the objects.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRelAssociatesClassification(IFCFile file, string globalId, IFCAnyHandle ownerHistory,
         string name, string description, HashSet<IFCAnyHandle> relatedObjects, IFCAnyHandle relatingClassification)
      {
         ValidateRelAssociates(globalId, ownerHistory, name, description, relatedObjects);

         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
            IFCAnyHandleUtil.ValidateSubTypeOf(relatingClassification, false, IFCEntityType.IfcClassificationReference);
         else
            IFCAnyHandleUtil.ValidateSubTypeOf(relatingClassification, false, IFCEntityType.IfcClassificationNotation, IFCEntityType.IfcClassificationReference);

         IFCAnyHandle relAssociatesClassification = CreateInstance(file, IFCEntityType.IfcRelAssociatesClassification, null);
         SetRelAssociates(relAssociatesClassification, globalId, ownerHistory, name, description, relatedObjects);
         IFCAnyHandleUtil.SetAttribute(relAssociatesClassification, "RelatingClassification", relatingClassification);
         return relAssociatesClassification;
      }

      /// <summary>
      /// Creates a handle representing an IfcCreateElementAssembly and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="globalId">The GUID of the IfcCreateElementAssembly.</param>
      /// <param name="ownerHistory">The owner history of the IfcCreateElementAssembly.</param>
      /// <param name="name">Name of the assembly.</param>
      /// <param name="description">Description of the assembly.</param>
      /// <param name="objectType">The object type of the assembly, usually the name of the Assembly type.</param>
      /// <param name="objectPlacement">The placement of the assembly.</param>
      /// <param name="representation">The representation of the assembly, usually empty.</param>
      /// <param name="tag">The tag of the assembly, usually represented by the Element ID.</param>
      /// <param name="assemblyPlace">The place where the assembly is made.</param>
      /// <param name="predefinedType">The type of the assembly, from a list of pre-defined types.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateElementAssembly(ExporterIFC exporterIFC, Element element, string globalId, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, IFCAssemblyPlace? assemblyPlace, IFCElementAssemblyType predefinedType)
      {
         ValidateElement(globalId, ownerHistory, objectPlacement, representation);

         IFCAnyHandle elementAssembly = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcElementAssembly, element);
         SetElement(exporterIFC, elementAssembly, element, globalId, ownerHistory, objectPlacement, representation);
         IFCAnyHandleUtil.SetAttribute(elementAssembly, "AssemblyPlace", assemblyPlace);
         IFCAnyHandleUtil.SetAttribute(elementAssembly, "PredefinedType", predefinedType);
         return elementAssembly;
      }

      /// <summary>
      /// Creates a handle representing an IfcBuildingElementPart and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID of the IfcBuildingElementPart.</param>
      /// <param name="ownerHistory">The owner history of the IfcBuildingElementPart.</param>
      /// <param name="name">Name of the entity.</param>
      /// <param name="description">Description of the entity.</param>
      /// <param name="objectType">Object type of the entity.</param>
      /// <param name="objectPlacement">Placement handle of the entity.</param>
      /// <param name="representation">Representation handle of the enti</param>
      /// <param name="elementTag">The element tag.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateBuildingElementPart(ExporterIFC exporterIFC, Element element, string guid, IFCAnyHandle ownerHistory,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation)
      {
         ValidateElement(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle part = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcBuildingElementPart, element);
         SetElement(exporterIFC, part, element, guid, ownerHistory, objectPlacement, representation);
         return part;
      }

      /// <summary>
      /// Creates a handle representing an IfcAnnotationFillArea and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="outerBoundary">The outer boundary.</param>
      /// <param name="innerBoundaries">The inner boundaries.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateAnnotationFillArea(IFCFile file, IFCAnyHandle outerBoundary, HashSet<IFCAnyHandle> innerBoundaries)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(outerBoundary, false, IFCEntityType.IfcCurve);
         IFCAnyHandleUtil.ValidateSubTypeOf(innerBoundaries, true, IFCEntityType.IfcCurve);

         IFCAnyHandle annotationFillArea = CreateInstance(file, IFCEntityType.IfcAnnotationFillArea, null);
         IFCAnyHandleUtil.SetAttribute(annotationFillArea, "OuterBoundary", outerBoundary);
         IFCAnyHandleUtil.SetAttribute(annotationFillArea, "InnerBoundaries", innerBoundaries);
         return annotationFillArea;
      }

      /// <summary>
      /// Creates a handle representing an IfcArbitraryClosedProfileDef and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="profileType">The profile type.</param>
      /// <param name="profileName">The profile name.</param>
      /// <param name="outerCurve">The profile curve.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateArbitraryClosedProfileDef(IFCFile file, IFCProfileType profileType, string profileName, IFCAnyHandle outerCurve)
      {
         ValidateArbitraryOpenOrClosedProfileDef(outerCurve);

         IFCAnyHandle arbitraryClosedProfileDef = CreateInstance(file, IFCEntityType.IfcArbitraryClosedProfileDef, null);
         SetArbitraryClosedProfileDef(arbitraryClosedProfileDef, profileType, profileName, outerCurve);
         return arbitraryClosedProfileDef;
      }

      /// <summary>
      /// Creates a handle representing an IfcArbitraryOpenProfileDef and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="profileType">The profile type.</param>
      /// <param name="profileName">The profile name.</param>
      /// <param name="curve">The profile curve.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateArbitraryOpenProfileDef(IFCFile file, IFCProfileType profileType, string profileName, IFCAnyHandle curve)
      {
         ValidateArbitraryOpenOrClosedProfileDef(curve);

         IFCAnyHandle arbitraryOpenProfileDef = CreateInstance(file, IFCEntityType.IfcArbitraryOpenProfileDef, null);
         SetProfileDef(arbitraryOpenProfileDef, profileType, profileName);
         IFCAnyHandleUtil.SetAttribute(arbitraryOpenProfileDef, "Curve", curve);

         return arbitraryOpenProfileDef;
      }

      /// <summary>
      /// Creates a handle representing an IfcArbitraryProfileDefWithVoids and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="profileType">The profile type.</param>
      /// <param name="profileName">The profile name.</param>
      /// <param name="positionHnd">The profile position.</param>
      /// <param name="radius">The profile radius.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateArbitraryProfileDefWithVoids(IFCFile file, IFCProfileType profileType, string profileName, IFCAnyHandle outerCurve,
          HashSet<IFCAnyHandle> innerCurves)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(innerCurves, false, IFCEntityType.IfcCurve);

         ValidateArbitraryOpenOrClosedProfileDef(outerCurve);

         IFCAnyHandle arbitraryProfileDefWithVoids = CreateInstance(file, IFCEntityType.IfcArbitraryProfileDefWithVoids, null);
         SetArbitraryClosedProfileDef(arbitraryProfileDefWithVoids, profileType, profileName, outerCurve);
         IFCAnyHandleUtil.SetAttribute(arbitraryProfileDefWithVoids, "InnerCurves", innerCurves);
         return arbitraryProfileDefWithVoids;
      }

      /// <summary>
      /// Creates a handle representing an IfcCircleProfileDef and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="profileType">The profile type.</param>
      /// <param name="profileName">The profile name.</param>
      /// <param name="positionHnd">The profile position.</param>
      /// <param name="radius">The profile radius.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateCircleProfileDef(IFCFile file, IFCProfileType profileType, string profileName, IFCAnyHandle positionHnd,
          double radius)
      {
         ValidateCircleProfileDef(positionHnd, radius);

         IFCAnyHandle circleProfileDef = CreateInstance(file, IFCEntityType.IfcCircleProfileDef, null);
         SetCircleProfileDef(circleProfileDef, profileType, profileName, positionHnd, radius);
         return circleProfileDef;
      }

      /// <summary>
      /// Creates a handle representing an IfcCircleHollowProfileDef and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="profileType">The profile type.</param>
      /// <param name="profileName">The profile name.</param>
      /// <param name="positionHnd">The profile position.</param>
      /// <param name="radius">The profile radius.</param>
      /// <param name="wallThickness">The wall thickness.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateCircleHollowProfileDef(IFCFile file, IFCProfileType profileType, string profileName, IFCAnyHandle positionHnd,
          double radius, double wallThickness)
      {
         if (wallThickness < MathUtil.Eps())
            throw new ArgumentException("Non-positive wall thickness parameter.", "wallThickness");

         ValidateCircleProfileDef(positionHnd, radius);

         IFCAnyHandle circleHollowProfileDef = CreateInstance(file, IFCEntityType.IfcCircleHollowProfileDef, null);
         SetCircleProfileDef(circleHollowProfileDef, profileType, profileName, positionHnd, radius);
         IFCAnyHandleUtil.SetAttribute(circleHollowProfileDef, "WallThickness", wallThickness);
         return circleHollowProfileDef;
      }

      /// <summary>
      /// Creates a handle representing an IfcRectangleProfileDef and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="profileType">The profile type.</param>
      /// <param name="profileName">The profile name.</param>
      /// <param name="positionHnd">The profile position.</param>
      /// <param name="xLen">The profile length.</param>
      /// <param name="yLen">The profile width.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateRectangleProfileDef(IFCFile file, IFCProfileType profileType, string profileName, IFCAnyHandle positionHnd,
          double length, double width)
      {
         if (length < MathUtil.Eps())
            throw new ArgumentException("Non-positive length parameter.", "length");
         if (width < MathUtil.Eps())
            throw new ArgumentException("Non-positive width parameter.", "width");

         ValidateParameterizedProfileDef(positionHnd);

         IFCAnyHandle rectangleProfileDef = CreateInstance(file, IFCEntityType.IfcRectangleProfileDef, null);
         SetParameterizedProfileDef(rectangleProfileDef, profileType, profileName, positionHnd);
         IFCAnyHandleUtil.SetAttribute(rectangleProfileDef, "XDim", length);
         IFCAnyHandleUtil.SetAttribute(rectangleProfileDef, "YDim", width);
         return rectangleProfileDef;
      }

      /// <summary>
      /// Creates a handle representing an IfcIShapeProfileDef and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="profileType">The profile type.</param>
      /// <param name="profileName">The profile name.</param>
      /// <param name="positionHnd">The profile position.</param>
      /// <param name="xLen">The profile length.</param>
      /// <param name="yLen">The profile width.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateIShapeProfileDef(IFCFile file, IFCProfileType profileType, string profileName, IFCAnyHandle positionHnd,
          double overallWidth, double overallDepth, double webThickness, double flangeThickness, double? filletRadius)
      {
         if (overallWidth < MathUtil.Eps())
            throw new ArgumentException("Non-positive width parameter.", "overallWidth");
         if (overallDepth < MathUtil.Eps())
            throw new ArgumentException("Non-positive depth parameter.", "overallDepth");
         if (webThickness < MathUtil.Eps())
            throw new ArgumentException("Non-positive web thickness parameter.", "webThickness");
         if (flangeThickness < MathUtil.Eps())
            throw new ArgumentException("Non-positive flange thickness parameter.", "flangeThickness");
         if ((filletRadius != null) && filletRadius.GetValueOrDefault() < MathUtil.Eps())
            throw new ArgumentException("Non-positive fillet radius parameter.", "filletRadius");

         ValidateParameterizedProfileDef(positionHnd);

         IFCAnyHandle iShapeProfileDef = CreateInstance(file, IFCEntityType.IfcIShapeProfileDef, null);
         SetParameterizedProfileDef(iShapeProfileDef, profileType, profileName, positionHnd);
         IFCAnyHandleUtil.SetAttribute(iShapeProfileDef, "OverallWidth", overallWidth);
         IFCAnyHandleUtil.SetAttribute(iShapeProfileDef, "OverallDepth", overallDepth);
         IFCAnyHandleUtil.SetAttribute(iShapeProfileDef, "WebThickness", webThickness);
         IFCAnyHandleUtil.SetAttribute(iShapeProfileDef, "FlangeThickness", flangeThickness);
         IFCAnyHandleUtil.SetAttribute(iShapeProfileDef, "FilletRadius", filletRadius);
         return iShapeProfileDef;
      }

      /// <summary>
      /// Creates a handle representing an IfcExtrudedAreaSolid and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="sweptArea">The profile.</param>
      /// <param name="solidAxis">The plane of the profile.</param>
      /// <param name="extrudedDirection">The extrusion direction.</param>
      /// <param name="depth">The extrusion depth.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateExtrudedAreaSolid(IFCFile file, IFCAnyHandle sweptArea, IFCAnyHandle solidAxis, IFCAnyHandle extrudedDirection,
          double depth)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(extrudedDirection, false, IFCEntityType.IfcDirection);
         if (depth < MathUtil.Eps())
            throw new ArgumentException("Non-positive depth parameter.", "depth");

         ValidateSweptAreaSolid(sweptArea, solidAxis);

         IFCAnyHandle extrudedAreaSolid = CreateInstance(file, IFCEntityType.IfcExtrudedAreaSolid, null);
         SetSweptAreaSolid(extrudedAreaSolid, sweptArea, solidAxis);
         IFCAnyHandleUtil.SetAttribute(extrudedAreaSolid, "ExtrudedDirection", extrudedDirection);
         IFCAnyHandleUtil.SetAttribute(extrudedAreaSolid, "Depth", depth);
         return extrudedAreaSolid;
      }

      /// <summary>
      /// Creates a handle representing an IfcSurfaceCurveSweptAreaSolid and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="sweptArea">The profile.</param>
      /// <param name="solidAxis">The plane of the profile.</param>
      /// <param name="directrix">The curve used to define the sweeping operation.</param>
      /// <param name="startParam">The start parameter of sweeping.</param>
      /// <param name="endParam">The end parameter of sweeping.</param>
      /// <param name="referencePlane">The surface containing the Directrix.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSurfaceCurveSweptAreaSolid(IFCFile file, IFCAnyHandle sweptArea, IFCAnyHandle solidAxis, IFCAnyHandle directrix,
          double startParam, double endParam, IFCAnyHandle referencePlane)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(directrix, false, IFCEntityType.IfcCurve);
         IFCAnyHandleUtil.ValidateSubTypeOf(referencePlane, false, IFCEntityType.IfcSurface);

         ValidateSweptAreaSolid(sweptArea, solidAxis);

         IFCAnyHandle surfaceCurveSweptAreaSolid = CreateInstance(file, IFCEntityType.IfcSurfaceCurveSweptAreaSolid, null);
         SetSweptAreaSolid(surfaceCurveSweptAreaSolid, sweptArea, solidAxis);
         IFCAnyHandleUtil.SetAttribute(surfaceCurveSweptAreaSolid, "Directrix", directrix);
         IFCAnyHandleUtil.SetAttribute(surfaceCurveSweptAreaSolid, "StartParam", startParam);
         IFCAnyHandleUtil.SetAttribute(surfaceCurveSweptAreaSolid, "EndParam", endParam);
         IFCAnyHandleUtil.SetAttribute(surfaceCurveSweptAreaSolid, "ReferenceSurface", referencePlane);
         return surfaceCurveSweptAreaSolid;
      }

      /// <summary>
      /// Creates a handle representing an IfcSurfaceOfLinearExtrusion and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="sweptCurve">The swept curve.</param>
      /// <param name="position">The position.</param>
      /// <param name="direction">The direction.</param>
      /// <param name="depth">The depth.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSurfaceOfLinearExtrusion(IFCFile file, IFCAnyHandle sweptCurve, IFCAnyHandle position, IFCAnyHandle direction,
          double depth)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(direction, false, IFCEntityType.IfcDirection);
         ValidateSweptAreaSolid(sweptCurve, position);

         IFCAnyHandle surfaceOfLinearExtrusion = CreateInstance(file, IFCEntityType.IfcSurfaceOfLinearExtrusion, null);
         SetSweptSurface(surfaceOfLinearExtrusion, sweptCurve, position);
         IFCAnyHandleUtil.SetAttribute(surfaceOfLinearExtrusion, "ExtrudedDirection", direction);
         IFCAnyHandleUtil.SetAttribute(surfaceOfLinearExtrusion, "Depth", depth);
         return surfaceOfLinearExtrusion;
      }

      /// <summary>
      /// Create Advanced Face: IfcSurfaceOfRevolution
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="sweptCurve">The swept curve</param>
      /// <param name="position">The position</param>
      /// <param name="axisPosition">The axis of revolution</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateSurfaceOfRevolution(IFCFile file, IFCAnyHandle sweptCurve, IFCAnyHandle position, IFCAnyHandle axisPosition)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(sweptCurve, false, IFCEntityType.IfcProfileDef);
         if (position != null)
            IFCAnyHandleUtil.ValidateSubTypeOf(position, false, IFCEntityType.IfcAxis2Placement3D);

         IFCAnyHandle revolvedFace = CreateInstance(file, IFCEntityType.IfcSurfaceOfRevolution, null);
         SetSweptSurface(revolvedFace, sweptCurve, position);
         IFCAnyHandleUtil.ValidateSubTypeOf(axisPosition, false, IFCEntityType.IfcAxis1Placement);
         IFCAnyHandleUtil.SetAttribute(revolvedFace, "AxisPosition", axisPosition);
         return revolvedFace;
      }

      /// <summary>
      /// Creates a handle representing an IfcSurfaceStyleRendering and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="surfaceColour">The optional surface colour.</param>
      /// <param name="transparency">The optional transparency.</param>
      /// <param name="diffuseColour">The optional diffuse colour, as a handle or normalised ratio.</param>
      /// <param name="transmissionColour">The optional transmission colour, as a handle or normalised ratio.</param>
      /// <param name="diffuseTransmissionColour">The optional diffuse transmission colour, as a handle or normalised ratio.</param>
      /// <param name="reflectionColour">The optional reflection colour, as a handle or normalised ratio.</param>
      /// <param name="specularColour">The optional specular colour, as a handle or normalised ratio.</param>
      /// <param name="specularHighlight">The optional specular highlight, as a handle or normalised ratio.</param>
      /// <param name="method">The reflectance method.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSurfaceStyleRendering(IFCFile file, IFCAnyHandle surfaceColour,
          double? transparency, IFCData diffuseColour,
          IFCData transmissionColour, IFCData diffuseTransmissionColour,
          IFCData reflectionColour, IFCData specularColour, IFCData specularHighlight, IFCReflectanceMethod method)
      {
         ValidSurfaceStyleShading(surfaceColour);

         IFCAnyHandle surfaceStyleRendering = CreateInstance(file, IFCEntityType.IfcSurfaceStyleRendering, null);
         SetSurfaceStyleShading(surfaceStyleRendering, surfaceColour);
         IFCAnyHandleUtil.SetAttribute(surfaceStyleRendering, "Transparency", transparency);
         IFCAnyHandleUtil.SetAttribute(surfaceStyleRendering, "DiffuseColour", diffuseColour);
         IFCAnyHandleUtil.SetAttribute(surfaceStyleRendering, "TransmissionColour", transmissionColour);
         IFCAnyHandleUtil.SetAttribute(surfaceStyleRendering, "DiffuseTransmissionColour", diffuseTransmissionColour);
         IFCAnyHandleUtil.SetAttribute(surfaceStyleRendering, "ReflectionColour", reflectionColour);
         IFCAnyHandleUtil.SetAttribute(surfaceStyleRendering, "SpecularColour", specularColour);
         IFCAnyHandleUtil.SetAttribute(surfaceStyleRendering, "SpecularHighlight", specularHighlight);
         IFCAnyHandleUtil.SetAttribute(surfaceStyleRendering, "ReflectanceMethod", method);

         return surfaceStyleRendering;
      }

      /// <summary>
      /// Creates a handle representing an IfcSurfaceStyle and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="side">The side of the surface being used.</param>
      /// <param name="styles">The styles associated with the surface.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateSurfaceStyle(IFCFile file, string name, IFCSurfaceSide side, ISet<IFCAnyHandle> styles)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(styles, false, IFCEntityType.IfcSurfaceStyleShading, IFCEntityType.IfcSurfaceStyleLighting,
             IFCEntityType.IfcSurfaceStyleRefraction, IFCEntityType.IfcSurfaceStyleWithTextures, IFCEntityType.IfcExternallyDefinedSurfaceStyle);

         IFCAnyHandle surfaceStyle = CreateInstance(file, IFCEntityType.IfcSurfaceStyle, null);
         SetPresentationStyle(surfaceStyle, name);
         IFCAnyHandleUtil.SetAttribute(surfaceStyle, "Side", side);
         IFCAnyHandleUtil.SetAttribute(surfaceStyle, "Styles", styles);
         return surfaceStyle;
      }

      /// <summary>
      /// Creates a handle representing an IfcCurveStyle and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="name">The name.</param>
      /// <param name="font">A curve style font which is used to present a curve.</param>
      /// <param name="width">A positive length measure in units of the presentation area for the width of a presented curve.</param>
      /// <param name="colour">The colour of the visible part of the curve.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateCurveStyle(IFCFile file, string name, IFCAnyHandle font, IFCData width, IFCAnyHandle colour)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(font, true, IFCEntityType.IfcPreDefinedCurveFont, IFCEntityType.IfcCurveStyleFont, IFCEntityType.IfcCurveStyleFontAndScaling);
         IFCAnyHandleUtil.ValidateSubTypeOf(colour, true, IFCEntityType.IfcColourSpecification, IFCEntityType.IfcPreDefinedColour);

         IFCAnyHandle curveStyle = CreateInstance(file, IFCEntityType.IfcCurveStyle, null);
         SetPresentationStyle(curveStyle, name);
         IFCAnyHandleUtil.SetAttribute(curveStyle, "CurveFont", font);
         IFCAnyHandleUtil.SetAttribute(curveStyle, "CurveWidth", width);
         IFCAnyHandleUtil.SetAttribute(curveStyle, "CurveColour", colour);
         return curveStyle;
      }

      /// <summary>
      /// Creates a handle representing an IfcHalfSpaceSoild and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="baseSurface">The clipping surface.</param>
      /// <param name="agreementFlag">True if the normal of the half space solid points away from the base extrusion, false otherwise.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateHalfSpaceSolid(IFCFile file, IFCAnyHandle baseSurface, bool agreementFlag)
      {
         ValidateHalfSpaceSolid(baseSurface);

         IFCAnyHandle halfSpaceSolidHnd = CreateInstance(file, IFCEntityType.IfcHalfSpaceSolid, null);
         SetHalfSpaceSolid(halfSpaceSolidHnd, baseSurface, agreementFlag);
         return halfSpaceSolidHnd;
      }

      /// <summary>
      /// Creates a handle representing an IfcBooleanClippingResult and assigns it to the file.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="clipOperator">The clipping operator.</param>
      /// <param name="firstOperand">The handle to be clipped.</param>
      /// <param name="secondOperand">The clipping handle.</param>
      /// <returns>The IfcBooleanClippingResult handle.</returns>
      public static IFCAnyHandle CreateBooleanClippingResult(IFCFile file, IFCBooleanOperator clipOperator,
          IFCAnyHandle firstOperand, IFCAnyHandle secondOperand)
      {
         ValidateBooleanResult(firstOperand, secondOperand);

         IFCAnyHandle booleanClippingResultHnd = CreateInstance(file, IFCEntityType.IfcBooleanClippingResult, null);
         SetBooleanResult(booleanClippingResultHnd, clipOperator, firstOperand, secondOperand);
         return booleanClippingResultHnd;
      }

      /// <summary>
      /// Creates a handle representing an IfcBooleanResult and assigns it to the file.
      /// </summary>
      /// <param name="file">The IFC file.</param>
      /// <param name="boolOperator">The boolean operator.</param>
      /// <param name="firstOperand">The first operand to be operated upon by the Boolean operation.</param>
      /// <param name="secondOperand">The second operand specified for the operation.</param>
      /// <returns>The IfcBooleanResult handle.</returns>
      public static IFCAnyHandle CreateBooleanResult(IFCFile file, IFCBooleanOperator boolOperator,
          IFCAnyHandle firstOperand, IFCAnyHandle secondOperand)
      {
         ValidateBooleanResult(firstOperand, secondOperand);

         IFCAnyHandle booleanResultHnd = CreateInstance(file, IFCEntityType.IfcBooleanResult, null);
         SetBooleanResult(booleanResultHnd, boolOperator, firstOperand, secondOperand);
         return booleanResultHnd;
      }

      public static IFCAnyHandle CreatePolygonalBoundedHalfSpace(IFCFile file, IFCAnyHandle position, IFCAnyHandle polygonalBoundary,
          IFCAnyHandle baseSurface, bool agreementFlag)
      {
         ValidateHalfSpaceSolid(baseSurface);
         IFCAnyHandleUtil.ValidateSubTypeOf(position, false, IFCEntityType.IfcAxis2Placement3D);
         IFCAnyHandleUtil.ValidateSubTypeOf(polygonalBoundary, false, IFCEntityType.IfcBoundedCurve);

         IFCAnyHandle polygonalBoundedHalfSpaceHnd = CreateInstance(file, IFCEntityType.IfcPolygonalBoundedHalfSpace, null);
         SetHalfSpaceSolid(polygonalBoundedHalfSpaceHnd, baseSurface, agreementFlag);
         IFCAnyHandleUtil.SetAttribute(polygonalBoundedHalfSpaceHnd, "Position", position);
         IFCAnyHandleUtil.SetAttribute(polygonalBoundedHalfSpaceHnd, "PolygonalBoundary", polygonalBoundary);
         return polygonalBoundedHalfSpaceHnd;
      }

      /// <summary>
      /// Creates a handle representing an IfcPlane and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="position">The plane coordinate system.</param>
      /// <returns>The IfcPlane handle.</returns>
      public static IFCAnyHandle CreatePlane(IFCFile file, IFCAnyHandle position)
      {
         ValidateElementarySurface(position);

         IFCAnyHandle planeHnd = CreateInstance(file, IFCEntityType.IfcPlane, null);
         SetElementarySurface(planeHnd, position);
         return planeHnd;
      }

      /// <summary>
      /// Create unbounded Advanced Face: IfcCylindricalSurface
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="position">the origin</param>
      /// <param name="radius">The radius of the cylinder</param>
      /// <returns>the handle</returns>
      public static IFCAnyHandle CreateCylindricalSurface(IFCFile file, IFCAnyHandle position, double radius)
      {
         ValidateElementarySurface(position);

         IFCAnyHandle cylindricalSurface = CreateInstance(file, IFCEntityType.IfcCylindricalSurface, null);
         SetElementarySurface(cylindricalSurface, position);
         IFCAnyHandleUtil.SetAttribute(cylindricalSurface, "Radius", radius);
         return cylindricalSurface;
      }

      /// <summary>
      /// Creates a handle representing an IfcActor and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name</param>
      /// <param name="description">The description</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="theActor">The actor.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateActor(IFCFile file, string guid, IFCAnyHandle ownerHistory,
          string name, string description, string objectType, IFCAnyHandle theActor)
      {
         ValidateActor(guid, ownerHistory, name, objectType, theActor);

         IFCAnyHandle actorHandle = CreateInstance(file, IFCEntityType.IfcActor, null);
         SetActor(actorHandle, guid, ownerHistory, name, description, objectType, theActor);
         return actorHandle;
      }

      /// <summary>
      /// Create a handle representing IfcActorRole and assign it to the file
      /// </summary>
      /// <param name="file">the file</param>
      /// <param name="roleStr">Role enum in string format</param>
      /// <param name="userDefinedRole">string for User Defined Role</param>
      /// <param name="description">description</param>
      /// <returns></returns>
      public static IFCAnyHandle CreateActorRole(IFCFile file, string roleStr, string userDefinedRole, string description)
      {

         IFCAnyHandle actorRole = CreateInstance(file, IFCEntityType.IfcActorRole, null);
         if (ExporterCacheManager.ExportOptionsCache.ExportAs4)
         {
            Revit.IFC.Export.Toolkit.IFC4.IFCRole roleEnum;
            if (!Enum.TryParse(roleStr, out roleEnum)) roleEnum = Revit.IFC.Export.Toolkit.IFC4.IFCRole.USERDEFINED;
            IFCAnyHandleUtil.SetAttribute(actorRole, "Role", roleEnum);
         }
         else
         {
            Revit.IFC.Export.Toolkit.IFCRoleEnum roleEnum;
            if (!Enum.TryParse(roleStr, out roleEnum)) roleEnum = Revit.IFC.Export.Toolkit.IFCRoleEnum.UserDefined;
            IFCAnyHandleUtil.SetAttribute(actorRole, "Role", roleEnum);
         }
         IFCAnyHandleUtil.SetAttribute(actorRole, "UserDefinedRole", userDefinedRole);
         IFCAnyHandleUtil.SetAttribute(actorRole, "Description", description);
         return actorRole;
      }

      /// Creates an IfcGrid and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="guid">The GUID.</param>
      /// <param name="ownerHistory">The owner history.</param>
      /// <param name="name">The name.</param>
      /// <param name="description">The description.</param>
      /// <param name="objectType">The object type.</param>
      /// <param name="objectPlacement">The object placement.</param>
      /// <param name="representation">The geometric representation of the entity.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateGrid(ExporterIFC exporterIFC, string guid, IFCAnyHandle ownerHistory, string name,
          IFCAnyHandle objectPlacement, IFCAnyHandle representation, IList<IFCAnyHandle> uAxes, IList<IFCAnyHandle> vAxes, IList<IFCAnyHandle> wAxes)
      {
         ValidateProduct(guid, ownerHistory, objectPlacement, representation);

         IFCAnyHandle grid = CreateInstance(exporterIFC.GetFile(), IFCEntityType.IfcGrid, null);
         IFCAnyHandleUtil.SetAttribute(grid, "UAxes", uAxes);
         IFCAnyHandleUtil.SetAttribute(grid, "VAxes", vAxes);
         IFCAnyHandleUtil.SetAttribute(grid, "wAxes", wAxes);

         SetProduct(exporterIFC, grid, null, guid, ownerHistory, objectPlacement, representation);
         setRootName(grid, name);
         return grid;
      }

      /// <summary>
      /// Creates an IfcGridAxis and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="axisTag">The AxisTag.</param>
      /// <param name="axisCurve">The curve handle of the grid axis.</param>
      /// <param name="sameSense">The SameSense.</param>
      /// <returns>The handle</returns>
      public static IFCAnyHandle CreateGridAxis(IFCFile file, string axisTag, IFCAnyHandle axisCurve, bool sameSense)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(axisCurve, false, IFCEntityType.IfcCurve);

         IFCAnyHandle gridAxis = CreateInstance(file, IFCEntityType.IfcGridAxis, null);
         if (axisTag != string.Empty)
         {
            IFCAnyHandleUtil.SetAttribute(gridAxis, "AxisTag", axisTag);
         }
         IFCAnyHandleUtil.SetAttribute(gridAxis, "AxisCurve", axisCurve);
         IFCAnyHandleUtil.SetAttribute(gridAxis, "SameSense", sameSense);
         return gridAxis;
      }

      /// <summary>
      /// Creates an IfcGridPlacement and assigns it to the file.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="placementLocation">The PlacementLocation.</param>
      /// <param name="placementRefDirection">The PlacementRefDirection.</param>
      /// <returns>The handle</returns>
      public static IFCAnyHandle CreateGridPlacement(IFCFile file, IFCAnyHandle placementLocation, IFCAnyHandle placementRefDirection)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(placementLocation, true, IFCEntityType.IfcObjectPlacement);

         IFCAnyHandle gridPlacement = CreateInstance(file, IFCEntityType.IfcGridPlacement, null);
         IFCAnyHandleUtil.SetAttribute(gridPlacement, "PlacementLocation", placementLocation);
         IFCAnyHandleUtil.SetAttribute(gridPlacement, "PlacementRefDirection", placementRefDirection);
         return gridPlacement;
      }
      /// <summary>
      /// Create IfcIndexedColourMap entity and assign it to the file
      /// </summary>
      /// <param name="file">The file</param>
      /// <param name="mappedTo">The associated IfcTesselatedFaceSet</param>
      /// <param name="opacity">The opacity</param>
      /// <param name="colours">the IfcColourRgbList entity</param>
      /// <param name="colourIndex">the colour index</param>
      /// <returns>the IfcIndexedColourMap entity</returns>
      public static IFCAnyHandle CreateIndexedColourMap(IFCFile file, IFCAnyHandle mappedTo, double? opacity, IFCAnyHandle colours, IList<int> colourIndex)
      {
         IFCAnyHandleUtil.ValidateSubTypeOf(mappedTo, false, IFCEntityType.IfcTessellatedFaceSet);
         IFCAnyHandleUtil.ValidateSubTypeOf(colours, false, IFCEntityType.IfcColourRgbList);

         IFCAnyHandle indexedColourMap = CreateInstance(file, IFCEntityType.IfcIndexedColourMap, null);
         IFCAnyHandleUtil.SetAttribute(indexedColourMap, "MappedTo", mappedTo);
         if (opacity.HasValue)
            IFCAnyHandleUtil.SetAttribute(indexedColourMap, "Opacity", opacity);
         IFCAnyHandleUtil.SetAttribute(indexedColourMap, "Colours", colours);
         IFCAnyHandleUtil.SetAttribute(indexedColourMap, "ColourIndex", colourIndex);
         return indexedColourMap;
      }

      /// <summary>
      /// Create IfcColourRgbList entity and assign it to the file
      /// </summary>
      /// <param name="file">the File</param>
      /// <param name="colourList">the ColourRgbList data</param>
      /// <returns>return IfcColourRgbList</returns>
      public static IFCAnyHandle CreateColourRgbList(IFCFile file, IList<IList<double>> colourList)
      {
         ValidateListOfList(colourList, false, "colourList");

         IFCAnyHandle colourRgbList = CreateInstance(file, IFCEntityType.IfcColourRgbList, null);
         IFCAnyHandleUtil.SetAttribute(colourRgbList, "ColourList", colourList, 1, null, 3, 3);
         return colourRgbList;
      }

      /// <summary>
      /// Create IfcCoordinateReferenceSystem
      /// </summary>
      /// <param name="file">the File</param>
      /// <param name="name">Coordinate reference system name</param>
      /// <param name="description">description</param>
      /// <param name="geodeticDatum">Geomdetic Datum</param>
      /// <param name="verticalDatum">Vertical Datum</param>
      /// <returns></returns>
      public static IFCAnyHandle ProjectedCRS(IFCFile file, string name, string description, string geodeticDatum, string verticalDatum,
            string mapProjection, string mapZone, IFCAnyHandle mapUnit)
      {
         IFCAnyHandle coordinateReferenceSystem = CreateInstance(file, IFCEntityType.IfcProjectedCRS, null);
         IFCAnyHandleUtil.SetAttribute(coordinateReferenceSystem, "Name", name);
         if (string.IsNullOrEmpty(description))
            IFCAnyHandleUtil.SetAttribute(coordinateReferenceSystem, "Description", description);
         if (string.IsNullOrEmpty(geodeticDatum))
            IFCAnyHandleUtil.SetAttribute(coordinateReferenceSystem, "GeodeticDatum", geodeticDatum);
         if (string.IsNullOrEmpty(verticalDatum))
            IFCAnyHandleUtil.SetAttribute(coordinateReferenceSystem, "VerticalDatum", verticalDatum);
         if (string.IsNullOrEmpty(verticalDatum))
            IFCAnyHandleUtil.SetAttribute(coordinateReferenceSystem, "MapProjection", mapProjection);
         if (string.IsNullOrEmpty(verticalDatum))
            IFCAnyHandleUtil.SetAttribute(coordinateReferenceSystem, "MapZone", mapZone);
         if (string.IsNullOrEmpty(verticalDatum))
            IFCAnyHandleUtil.SetAttribute(coordinateReferenceSystem, "MapUnit", mapUnit);

         return coordinateReferenceSystem;
      }

      private static bool IsDeprecatedType(string theEnumType, string validatedString)
      {
         if (ExporterCacheManager.ExportOptionsCache.ExportAsOlderThanIFC4)
            return false;

         if (string.Compare(theEnumType, "IfcWallType", true) == 0)
         {
            if ((string.Compare(validatedString, "STANDARD", true) == 0) ||
               (string.Compare(validatedString, "POLYGONAL", true) == 0) ||
               (string.Compare(validatedString, "ELEMENTEDWALL", true) == 0))
               return true;
         }

         return false;
      }

      private static void SetSpecificEnumAttr(IFCAnyHandle elemHnd, string attributeNane, string predefTypeStr, string theEnumType)
      {
         string validatedType = IFCValidateEntry.GetValidIFCPredefinedType(predefTypeStr, theEnumType);
         if (string.IsNullOrEmpty(validatedType))
            validatedType = "NOTDEFINED";

         // This is for Enum value that is still in the list but has been deprecated
         if (IsDeprecatedType(theEnumType, validatedType))
            validatedType = "NOTDEFINED";

         // In some cases, NOTDEFINED enum is not defined. Ignore error in this case
         try
         {
            IFCAnyHandleUtil.SetAttribute(elemHnd, attributeNane, validatedType, true);
         }
         catch { }
      }
      #endregion

      #region public header creation methods

      /// <summary>
      /// Creates a handle representing file schema in the header.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFileSchema(IFCFile file)
      {
         return file.CreateHeaderInstance("file_schema");
      }

      /// <summary>
      /// Creates a handle representing file description section in the header.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="descriptions">The description strings.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFileDescription(IFCFile file, IList<string> descriptions)
      {
         IFCAnyHandle fileDescription = file.CreateHeaderInstance("file_description");
         IFCAnyHandleUtil.SetAttribute(fileDescription, "description", descriptions);
         return fileDescription;
      }

      /// <summary>
      /// Creates a handle representing file name section in the header.
      /// </summary>
      /// <param name="file">The file.</param>
      /// <param name="name">The name for the file.</param>
      /// <param name="author">The author list.</param>
      /// <param name="organization">The organization list.</param>
      /// <param name="preprocessorVersion">The preprocessor version.</param>
      /// <param name="originatingSystem">The orginating system.</param>
      /// <param name="authorisation">The authorisation.</param>
      /// <returns>The handle.</returns>
      public static IFCAnyHandle CreateFileName(IFCFile file, string name, IList<string> author, IList<string> organization, string preprocessorVersion,
          string originatingSystem, string authorisation)
      {
         IFCAnyHandle fileName = file.CreateHeaderInstance("file_name");
         IFCAnyHandleUtil.SetAttribute(fileName, "name", name);
         IFCAnyHandleUtil.SetAttribute(fileName, "author", author);
         IFCAnyHandleUtil.SetAttribute(fileName, "organisation", organization);
         IFCAnyHandleUtil.SetAttribute(fileName, "preprocessor_version", preprocessorVersion);
         IFCAnyHandleUtil.SetAttribute(fileName, "originating_system", originatingSystem);
         IFCAnyHandleUtil.SetAttribute(fileName, "authorisation", authorisation);
         return fileName;
      }

      #endregion
   }
}