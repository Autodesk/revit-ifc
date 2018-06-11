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
   public class IFCSurfaceStyle : IFCPresentationStyle
   {
      private IFCSurfaceSide m_SurfaceSide = IFCSurfaceSide.Both;

      private IFCSurfaceStyleShading m_ShadingStyle = null;

      private ElementId m_CreatedElementId = ElementId.InvalidElementId;

      /// <summary>
      /// Get the IFCSurfaceStyleShading, if it is set.
      /// </summary>
      public IFCSurfaceStyleShading ShadingStyle
      {
         get { return m_ShadingStyle; }
         protected set { m_ShadingStyle = value; }
      }

      /// <summary>
      /// Get the side for which the style is set: inside, outside, or both.
      /// </summary>
      public IFCSurfaceSide SurfaceSide
      {
         get { return m_SurfaceSide; }
         protected set { m_SurfaceSide = value; }
      }

      protected IFCSurfaceStyle()
      {
      }

      override protected void Process(IFCAnyHandle item)
      {
         base.Process(item);

         SurfaceSide = IFCEnums.GetSafeEnumerationAttribute<IFCSurfaceSide>(item, "Side", IFCSurfaceSide.Both);

         HashSet<IFCAnyHandle> styles = IFCAnyHandleUtil.GetAggregateInstanceAttribute<HashSet<IFCAnyHandle>>(item, "Styles");
         if (styles == null || styles.Count == 0)
            Importer.TheLog.LogError(item.StepId, "No style information found, ignoring.", true);

         foreach (IFCAnyHandle style in styles)
         {
            try
            {
               if (IFCAnyHandleUtil.IsSubTypeOf(style, IFCEntityType.IfcSurfaceStyleShading))
               {
                  if (ShadingStyle == null)
                     ShadingStyle = IFCSurfaceStyleShading.ProcessIFCSurfaceStyleShading(style);
                  else
                     Importer.TheLog.LogWarning(item.StepId, "Duplicate IfcSurfaceStyleShading, ignoring.", false);
               }
               else
                  Importer.TheLog.LogUnhandledSubTypeError(style, "IfcSurfaceStyleElementSelect", false);
            }
            catch (Exception ex)
            {
               Importer.TheLog.LogError(style.StepId, ex.Message, false);
            }
         }
      }

      protected IFCSurfaceStyle(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Create the material associated to the element, and return the id.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="forcedName">An optional name that sets the name of the material created, regardless of surface style name.</param>
      /// <param name="suggestedName">An optional name that suggests the name of the material created, if the surface style name is null.</param>
      /// <param name="idOverride">The id of the parent item, used if forcedName is used.</param>
      /// <returns>The material id.</returns>
      /// <remarks>If forcedName is not null, this will not store the created element id in this class.</remarks>
      public ElementId Create(Document doc, string forcedName, string suggestedName, int idOverride)
      {
         try
         {
            bool overrideName = (forcedName != null) && (string.Compare(forcedName, Name) != 0);
            if (!overrideName && m_CreatedElementId != ElementId.InvalidElementId)
               return m_CreatedElementId;

            string name = overrideName ? forcedName : Name;
            if (string.IsNullOrEmpty(name))
            {
               if (!string.IsNullOrEmpty(suggestedName))
                  name = suggestedName;
               else
                  name = "IFC Surface Style";
            }
            int id = overrideName ? idOverride : Id;

            if (IsValidForCreation)
            {
               Color color = null;
               int? transparency = null;
               int? shininess = null;
               int? smoothness = null;

               IFCSurfaceStyleShading shading = ShadingStyle;
               if (shading != null)
               {
                  color = shading.GetSurfaceColor();
                  transparency = (int)(shading.Transparency * 100 + 0.5);
                  shininess = shading.GetShininess();
                  smoothness = shading.GetSmoothness();
               }

               IFCMaterialInfo materialInfo =
                   IFCMaterialInfo.Create(color, transparency, shininess, smoothness, ElementId.InvalidElementId);
               ElementId createdElementId = IFCMaterial.CreateMaterialElem(doc, id, name, materialInfo);
               if (!overrideName)
                  m_CreatedElementId = createdElementId;
               return createdElementId;
            }
            else
               IsValidForCreation = false;
         }
         catch (Exception ex)
         {
            IsValidForCreation = false;
            Importer.TheLog.LogCreationError(this, ex.Message, false);
         }

         return ElementId.InvalidElementId;
      }

      /// <summary>
      /// Processes an IfcSurfaceStyle entity handle.
      /// </summary>
      /// <param name="ifcSurfaceStyle">The IfcSurfaceStyle handle.</param>
      /// <returns>The IFCSurfaceStyle object.</returns>
      public static IFCSurfaceStyle ProcessIFCSurfaceStyle(IFCAnyHandle ifcSurfaceStyle)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSurfaceStyle))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSurfaceStyle);
            return null;
         }

         IFCEntity surfaceStyle;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSurfaceStyle.StepId, out surfaceStyle))
            surfaceStyle = new IFCSurfaceStyle(ifcSurfaceStyle);
         return (surfaceStyle as IFCSurfaceStyle);
      }
   }
}