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
   /// Class representing an IfcColourRgb entity.
   /// </summary>
   public class IFCColourRgb : IFCColourSpecification
   {
      private double m_NormalisedRed = 0.5;

      private double m_NormalisedGreen = 0.5;

      private double m_NormalisedBlue = 0.5;

      protected IFCColourRgb()
      {
      }

      /// <summary>
      /// Get the "raw" normalised red value.
      /// </summary>
      public double NormalisedRed
      {
         get { return m_NormalisedRed; }
         protected set { m_NormalisedRed = value; }
      }

      /// <summary>
      /// Get the "raw" normalised blue value.
      /// </summary>
      public double NormalisedBlue
      {
         get { return m_NormalisedBlue; }
         protected set { m_NormalisedBlue = value; }
      }

      /// <summary>
      /// Get the "raw" normalised green value.
      /// </summary>
      public double NormalisedGreen
      {
         get { return m_NormalisedGreen; }
         protected set { m_NormalisedGreen = value; }
      }

      /// <summary>
      /// Get the RGB associated to the color.
      /// </summary>
      /// <returns>The Color value.</returns>
      protected override Color CreateColor()
      {
         byte red = (byte)(NormalisedRed * 255 + 0.5);
         byte green = (byte)(NormalisedGreen * 255 + 0.5);
         byte blue = (byte)(NormalisedBlue * 255 + 0.5);
         return new Color(red, green, blue);
      }

      /// <summary>
      /// Get the RGB associated to the color, scaled by a normalised factor.
      /// </summary>
      /// <param name="factor">The normalised factor from 0 to 1.</param>
      /// <returns>The Color value.</returns>
      public Color GetScaledColor(double factor)
      {
         if (factor < MathUtil.Eps())
         {
            Importer.TheLog.LogWarning(Id, "Invalid negative scaling factor of " + factor + ", defaulting to black.", true);
            return new Color(0, 0, 0);
         }

         Color origColor = GetColor();
         if (origColor == null)
         {
            Importer.TheLog.LogError(Id, "Couldn't create color, default to grey.", false);
            return new Color(127, 127, 127);
         }

         if (factor > 1.0 + MathUtil.Eps())
         {
            Importer.TheLog.LogWarning(Id, "Invalid normalised scaling factor of " + factor + ", defaulting to original color", true);
            return origColor;
         }

         byte red = (byte)(origColor.Red * 255 * factor + 0.5);
         byte green = (byte)(origColor.Green * 255 * factor + 0.5);
         byte blue = (byte)(origColor.Blue * 255 * factor + 0.5);
         return new Color(red, green, blue);
      }

      override protected void Process(IFCAnyHandle item)
      {
         base.Process(item);

         NormalisedRed = IFCImportHandleUtil.GetOptionalNormalisedRatioAttribute(item, "Red", 0.5);
         NormalisedGreen = IFCImportHandleUtil.GetOptionalNormalisedRatioAttribute(item, "Green", 0.5);
         NormalisedBlue = IFCImportHandleUtil.GetOptionalNormalisedRatioAttribute(item, "Blue", 0.5);
      }

      protected IFCColourRgb(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Processes an IfcColourRgb entity handle.
      /// </summary>
      /// <param name="ifcColourRgb">The IfcColourRgb handle.</param>
      /// <returns>The IFCColourRgb object.</returns>
      public static IFCColourRgb ProcessIFCColourRgb(IFCAnyHandle ifcColourRgb)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcColourRgb))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcColourRgb);
            return null;
         }

         IFCEntity colourRgb;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcColourRgb.StepId, out colourRgb))
            colourRgb = new IFCColourRgb(ifcColourRgb);
         return (colourRgb as IFCColourRgb);
      }
   }
}