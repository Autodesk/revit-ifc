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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Utility;
using Revit.IFC.Import.Properties;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class to represent materials in IFC files.
   /// </summary>
   public class IFCMaterial : IFCEntity, IIFCMaterialSelect
   {
      private string m_Name = null;

      private IFCProductRepresentation m_MaterialDefinitionRepresentation = null;

      private ElementId m_CreatedElementId = ElementId.InvalidElementId;

      /// <summary>
      /// The name of the material.
      /// </summary>
      public string Name
      {
         get { return m_Name; }
         protected set { m_Name = value; }
      }

      /// <summary>
      /// The associated representation of the material.
      /// </summary>
      public IFCProductRepresentation MaterialDefinitionRepresentation
      {
         get { return m_MaterialDefinitionRepresentation; }
         protected set { m_MaterialDefinitionRepresentation = value; }
      }

      /// <summary>
      /// Returns the main element id associated with this material.
      /// </summary>
      public ElementId GetMaterialElementId()
      {
         if (m_CreatedElementId == ElementId.InvalidElementId && IsValidForCreation)
            Create(IFCImportFile.TheFile.Document);
         return m_CreatedElementId;
      }

      protected IFCMaterial()
      {
      }

      protected IFCMaterial(IFCAnyHandle ifcMaterial)
      {
         Process(ifcMaterial);
      }

      protected override void Process(IFCAnyHandle ifcMaterial)
      {
         base.Process(ifcMaterial);

         Name = IFCImportHandleUtil.GetRequiredStringAttribute(ifcMaterial, "Name", true);

         List<IFCAnyHandle> hasRepresentation = null;
         if (IFCImportFile.TheFile.SchemaVersionAtLeast(IFCSchemaVersion.IFC2x3))
            hasRepresentation = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcMaterial, "HasRepresentation");

         if ((hasRepresentation?.Count ?? 0) == 1)
         {
            if (!IFCAnyHandleUtil.IsSubTypeOf(hasRepresentation[0], IFCEntityType.IfcMaterialDefinitionRepresentation))
               Importer.TheLog.LogUnexpectedTypeError(hasRepresentation[0], IFCEntityType.IfcMaterialDefinitionRepresentation, false);
            else
               MaterialDefinitionRepresentation = IFCProductRepresentation.ProcessIFCProductRepresentation(hasRepresentation[0]);
         }

         Importer.TheLog.AddToElementCount();
      }

      private static string GetMaterialName(int id, string originalName)
      {
         // Disallow creating multiple materials with the same name.  This means that the
         // same material, with different styles, will be created with different names.
         string materialName = Importer.TheCache.CreatedMaterials.GetUniqueMaterialName(originalName, id);

         string revitMaterialName = IFCNamingUtil.CleanIFCName(materialName);
         if (revitMaterialName != null)
            return revitMaterialName;

         return string.Format(Resources.IFCDefaultMaterialName, id);
      }

      /// <summary>
      /// Return the material list for this IFCMaterialSelect.
      /// </summary>
      public IList<IFCMaterial> GetMaterials()
      {
         IList<IFCMaterial> materials = new List<IFCMaterial>() { this };
         return materials;
      }

      /// <summary>
      /// Create a Revit Material.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="id">The id of the IFCEntity, used to avoid creating duplicate material names.</param>
      /// <param name="originalName">The base name of the material.</param>
      /// <param name="materialInfo">The material information.</param>
      /// <returns>The element id.</returns>
      public static ElementId CreateMaterialElem(Document doc, int id, string originalName, IFCMaterialInfo materialInfo)
      {
         ElementId createdElementId = Importer.TheCache.CreatedMaterials.FindMatchingMaterial(originalName, id, materialInfo);
         if (createdElementId != ElementId.InvalidElementId)
            return createdElementId;

         if (!materialInfo.IsValid())
            return ElementId.InvalidElementId;

         string revitMaterialName = GetMaterialName(id, originalName);

         createdElementId = Material.Create(doc, revitMaterialName);
         if (createdElementId == ElementId.InvalidElementId)
            return createdElementId;

         materialInfo.ElementId = createdElementId;
         Importer.TheCache.CreatedMaterials.Add(originalName, materialInfo);

         // Get info.
         Material materialElem = doc.GetElement(createdElementId) as Material;
         if (materialElem == null)
            return ElementId.InvalidElementId;

         bool materialHasValidColor = false;

         // We don't want an invalid value set below to prevent creating an element; log the message and move on.
         try
         {
            if (materialInfo.Color != null)
            {
               materialElem.Color = materialInfo.Color;
               materialHasValidColor = true;
            }
            else
            {
               materialElem.Color = new Color(127, 127, 127);
            }

            if (materialInfo.Transparency.HasValue)
               materialElem.Transparency = materialInfo.Transparency.Value;

            if (materialInfo.Shininess.HasValue)
               materialElem.Shininess = materialInfo.Shininess.Value;

            if (materialInfo.Smoothness.HasValue)
               materialElem.Smoothness = materialInfo.Smoothness.Value;
         }
         catch (Exception ex)
         {
            Importer.TheLog.LogError(id, "Couldn't set some Material values: " + ex.Message, false);
         }

         if (!materialHasValidColor)
            Importer.TheCache.MaterialsWithNoColor.Add(createdElementId);

         if (Importer.TheOptions.VerboseLogging)
         {
            string comment = "Created Material: " + revitMaterialName
                + " with color: (" + materialElem.Color.Red + ", " + materialElem.Color.Green + ", " + materialElem.Color.Blue + ") "
                + "transparency: " + materialElem.Transparency + " shininess: " + materialElem.Shininess + " smoothness: " + materialElem.Smoothness;
            Importer.TheLog.LogComment(id, comment, false);
         }

         Importer.TheLog.AddCreatedMaterial(doc, createdElementId);
         return createdElementId;
      }

      /// <summary>
      /// Traverse through the MaterialDefinitionRepresentation to get the style information relevant to the material.
      /// </summary>
      /// <returns>The one IFCSurfaceStyle.</returns>
      private IFCSurfaceStyle GetSurfaceStyle()
      {
         if (MaterialDefinitionRepresentation != null)
            return MaterialDefinitionRepresentation.GetSurfaceStyle();

         return null;
      }

      /// <summary>
      /// Creates a Revit material based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      public void Create(Document doc)
      {
         // TODO: support cut pattern id and cut pattern color.
         try
         {
            string name = Name;
            if (string.IsNullOrEmpty(name))
               name = String.Format(Resources.IFCDefaultMaterialName, Id);

            if (m_CreatedElementId == ElementId.InvalidElementId && IsValidForCreation)
            {
               IFCSurfaceStyle surfaceStyle = GetSurfaceStyle();
               if (surfaceStyle != null)
                  m_CreatedElementId = surfaceStyle.Create(doc, name, null, Id);
               else
               {
                  IFCMaterialInfo materialInfo = IFCMaterialInfo.Create(null, null, null, null, ElementId.InvalidElementId);
                  m_CreatedElementId = CreateMaterialElem(doc, Id, Name, materialInfo);
               }
            }
            else
            {
               IsValidForCreation = false;
            }
         }
         catch (Exception ex)
         {
            IsValidForCreation = false;
            Importer.TheLog.LogCreationError(this, ex.Message, false);
         }
      }

      /// <summary>
      /// Processes an IfcMaterial object.
      /// </summary>
      /// <param name="ifcMaterial">The IfcMaterial handle.</param>
      /// <returns>The IFCMaterial object.</returns>
      public static IFCMaterial ProcessIFCMaterial(IFCAnyHandle ifcMaterial)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcMaterial))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcMaterial);
            return null;
         }

         IFCEntity material;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcMaterial.StepId, out material))
            material = new IFCMaterial(ifcMaterial);
         return (material as IFCMaterial);
      }
   }
}