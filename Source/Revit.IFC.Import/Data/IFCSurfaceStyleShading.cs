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
   /// Class for IfcSurfaceStyleShading and subtype IfcSurfaceStyleRendering.
   /// </summary>
   public class IFCSurfaceStyleShading : IFCEntity
   {
      private IFCColourRgb m_SurfaceColour = null;

      private double m_Transparency = 0.0;

      private IFCColourRgb m_DiffuseColour = null;
      private double? m_DiffuseColourFactor = null;

      private IFCColourRgb m_TransmissionColour = null;
      private double? m_TransmissionColourFactor = null;

      private IFCColourRgb m_DiffuseTransmissionColour = null;
      private double? m_DiffuseTransmissionColourFactor = null;

      private IFCColourRgb m_ReflectionColour = null;
      private double? m_ReflectionColourFactor = null;

      private IFCColourRgb m_SpecularColour = null;
      private double? m_SpecularColourFactor = null;

      // Only one of these two will be set.
      private double? m_SpecularExponent = null;
      private double? m_SpecularRoughness = null;

      // TODO: handle.

      //ReflectanceMethod   :   IfcReflectanceMethodEnum;  

      protected IFCSurfaceStyleShading()
      {
      }

      /// <summary>
      /// Return the surface color of the shading style.
      /// </summary>
      public Color GetSurfaceColor()
      {
         if (m_SurfaceColour != null)
            return m_SurfaceColour.GetColor();

         // Default to gray.
         return new Color(127, 127, 127);
      }

      private Color GetDefaultColor()
      {
         // Default to gray.
         return new Color(127, 127, 127);
      }

      /// <summary>
      /// Return the surface color of the shading style, scaled by a normalised factor.
      /// </summary>
      public Color GetSurfaceColor(double factor)
      {
         if (m_SurfaceColour != null)
            return m_SurfaceColour.GetScaledColor(factor);
         return GetDefaultColor();
      }

      /// <summary>
      /// Return the diffuse color of the shading style.
      /// </summary>
      public Color GetDiffuseColor()
      {
         if (m_DiffuseColour != null)
            return m_DiffuseColour.GetColor();
         if (m_DiffuseColourFactor.HasValue)
            return GetSurfaceColor(m_DiffuseColourFactor.Value);
         return GetDefaultColor();
      }

      /// <summary>
      /// Return the transmission color of the shading style.
      /// </summary>
      public Color GetTransmissionColor()
      {
         if (m_TransmissionColour != null)
            return m_TransmissionColour.GetColor();
         if (m_TransmissionColourFactor.HasValue)
            return GetSurfaceColor(m_TransmissionColourFactor.Value);
         return GetDefaultColor();
      }

      /// <summary>
      /// Return the diffuse transmission color of the shading style.
      /// </summary>
      public Color GetDiffuseTransmissionColor()
      {
         if (m_DiffuseTransmissionColour != null)
            return m_DiffuseTransmissionColour.GetColor();
         if (m_DiffuseTransmissionColourFactor.HasValue)
            return GetSurfaceColor(m_DiffuseTransmissionColourFactor.Value);
         return GetDefaultColor();
      }

      /// <summary>
      /// Return the reflection color of the shading style.
      /// </summary>
      public Color GetReflectionColor()
      {
         if (m_ReflectionColour != null)
            return m_ReflectionColour.GetColor();
         if (m_ReflectionColourFactor.HasValue)
            return GetSurfaceColor(m_ReflectionColourFactor.Value);
         return GetDefaultColor();
      }

      /// <summary>
      /// Return the specular color of the shading style.
      /// </summary>
      public Color GetSpecularColor()
      {
         if (m_SpecularColour != null)
            return m_SpecularColour.GetColor();
         if (m_SpecularColourFactor.HasValue)
            return GetSurfaceColor(m_SpecularColourFactor.Value);
         return GetDefaultColor();
      }

      /// <summary>
      /// Returns the transparency of the shading style - 1.0 means completely transparent.
      /// </summary>
      public double Transparency
      {
         get { return m_Transparency; }
         protected set { m_Transparency = value; }
      }

      /// <summary>
      /// Calculates Revit shininess for a material based on the specular colour, if specified.
      /// </summary>
      public int? GetSmoothness()
      {
         if (m_SpecularColourFactor != null)
            return (int)(m_SpecularColourFactor * 100 + 0.5);
         if (m_SpecularColour == null)
            return null;

         // heuristic: get average of three components.
         double ave = (m_SpecularColour.NormalisedRed + m_SpecularColour.NormalisedBlue + m_SpecularColour.NormalisedGreen) / 3.0;
         return (int)(ave * 100 + 0.5);
      }

      /// <summary>
      /// Calculates Revit shininess for a material based on the specular highlight, if specified.
      /// </summary>
      public int? GetShininess()
      {
         int shininess = 0;
         string warning = null;

         // Assumes that m_SpecularExponent or m_SpecularShininess is set.
         // Validates that the value is in the range [0,128].
         if (m_SpecularExponent == null)
         {
            if (m_SpecularRoughness == null)
               return null;

            // m_SpecularRoughness is a real from [0,1] and is the reverse of our shininess.
            shininess = (int)((1.0 - m_SpecularRoughness.Value) * 128 + 0.5);

            if ((shininess < 0) || (shininess > 128))
               warning = "Specular Roughness of " + m_SpecularRoughness.Value + " is of out range, should be between 0 and 1.";
         }
         else
         {
            shininess = (int)(m_SpecularExponent.Value);

            if ((shininess < 0) || (shininess > 128))
               warning = "Specular Exponent of " + m_SpecularExponent.Value + " is of out range, should be between 0 and 128.";
         }


         if (shininess < 0)
            shininess = 0;
         else if (shininess > 128)
            shininess = 128;

         if (warning != null)
            Importer.TheLog.LogWarning(Id, warning, true);

         return shininess;
      }

      override protected void Process(IFCAnyHandle item)
      {
         base.Process(item);

         IFCAnyHandle surfaceColour = IFCImportHandleUtil.GetRequiredInstanceAttribute(item, "SurfaceColour", false);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(surfaceColour))
            m_SurfaceColour = IFCColourRgb.ProcessIFCColourRgb(surfaceColour);

         if (IFCAnyHandleUtil.IsSubTypeOf(item, IFCEntityType.IfcSurfaceStyleRendering))
         {
            Transparency = IFCImportHandleUtil.GetOptionalNormalisedRatioAttribute(item, "Transparency", 0.0);

            IFCData diffuseColour = item.GetAttribute("DiffuseColour");
            if (diffuseColour.PrimitiveType == IFCDataPrimitiveType.Instance)
               m_DiffuseColour = IFCColourRgb.ProcessIFCColourRgb(diffuseColour.AsInstance());
            else if (diffuseColour.PrimitiveType == IFCDataPrimitiveType.Double)
               m_DiffuseColourFactor = diffuseColour.AsDouble();

            IFCData transmissionColour = item.GetAttribute("TransmissionColour");
            if (transmissionColour.PrimitiveType == IFCDataPrimitiveType.Instance)
               m_TransmissionColour = IFCColourRgb.ProcessIFCColourRgb(transmissionColour.AsInstance());
            else if (transmissionColour.PrimitiveType == IFCDataPrimitiveType.Double)
               m_TransmissionColourFactor = transmissionColour.AsDouble();

            IFCData diffuseTransmissionColour = item.GetAttribute("DiffuseTransmissionColour");
            if (diffuseTransmissionColour.PrimitiveType == IFCDataPrimitiveType.Instance)
               m_DiffuseTransmissionColour = IFCColourRgb.ProcessIFCColourRgb(diffuseTransmissionColour.AsInstance());
            else if (diffuseTransmissionColour.PrimitiveType == IFCDataPrimitiveType.Double)
               m_DiffuseTransmissionColourFactor = diffuseTransmissionColour.AsDouble();

            IFCData reflectionColour = item.GetAttribute("ReflectionColour");
            if (reflectionColour.PrimitiveType == IFCDataPrimitiveType.Instance)
               m_ReflectionColour = IFCColourRgb.ProcessIFCColourRgb(reflectionColour.AsInstance());
            else if (reflectionColour.PrimitiveType == IFCDataPrimitiveType.Double)
               m_ReflectionColourFactor = reflectionColour.AsDouble();

            IFCData specularColour = item.GetAttribute("SpecularColour");
            if (specularColour.PrimitiveType == IFCDataPrimitiveType.Instance)
               m_SpecularColour = IFCColourRgb.ProcessIFCColourRgb(specularColour.AsInstance());
            else if (specularColour.PrimitiveType == IFCDataPrimitiveType.Double)
               m_SpecularColourFactor = specularColour.AsDouble();

            IFCData specularHighlight = item.GetAttribute("SpecularHighlight");
            if (specularHighlight.PrimitiveType == IFCDataPrimitiveType.Double)
            {
               try
               {
                  string simpleType = specularHighlight.GetSimpleType();
                  if (string.Compare(simpleType, "IfcSpecularExponent", true) == 0)
                     m_SpecularExponent = specularHighlight.AsDouble();
                  else if (string.Compare(simpleType, "IfcSpecularRoughness", true) == 0)
                     m_SpecularRoughness = specularHighlight.AsDouble();
                  else
                     Importer.TheLog.LogError(item.StepId, "Unknown type of specular highlight, ignoring.", false);
               }
               catch
               {
                  Importer.TheLog.LogError(item.StepId, "Unspecified type of specular highlight, ignoring.", false);
               }
            }
            else if (specularHighlight.HasValue)
            {
               Importer.TheLog.LogError(item.StepId, "Unknown type of specular highlight, ignoring.", false);
            }
         }
      }

      protected IFCSurfaceStyleShading(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Processes an IfcSurfaceStyleShading entity handle.
      /// </summary>
      /// <param name="ifcSurfaceStyleShading">The IfcSurfaceStyleShading handle.</param>
      /// <returns>The IFCSurfaceStyleShading object.</returns>
      public static IFCSurfaceStyleShading ProcessIFCSurfaceStyleShading(IFCAnyHandle ifcSurfaceStyleShading)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSurfaceStyleShading))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSurfaceStyleShading);
            return null;
         }

         IFCEntity surfaceStyleShading;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcSurfaceStyleShading.StepId, out surfaceStyleShading))
            surfaceStyleShading = new IFCSurfaceStyleShading(ifcSurfaceStyleShading);
         return (surfaceStyleShading as IFCSurfaceStyleShading);
      }
   }
}