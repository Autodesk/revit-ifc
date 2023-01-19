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

using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Utility;
using System;

namespace Revit.IFC.Import.Data
{
   public class IFCColourRgbList : IFCPresentationItem
   {
      /// <summary>
      /// List of Colours.
      /// Each Colour is a triple (Red, Green, Blue).  As per IFC4 spec, this information is stored as a list of lists.
      /// There are no restrictions on the outer list, but the inner list must have three and only three entries.
      /// </summary>
      public IList<IList<double>> ColourList { get; protected set; } = null;

      /// <summary>
      /// Allows easy access to each Component of a given Colour in the ColourList.
      /// </summary>
      private enum ColorComponent
      {
         Red = 0,
         Green = 1,
         Blue = 2
      }

      protected IFCColourRgbList()
      {
      }

      protected IFCColourRgbList (IFCAnyHandle item)
      {
         Process(item);
      }

      protected override void Process (IFCAnyHandle ifcColourRgbList)
      {
         base.Process(ifcColourRgbList);

         IList<IList<double>> colourList = IFCImportHandleUtil.GetListOfListOfDoubleAttribute(ifcColourRgbList, "ColourList");
         if ((colourList?.Count ?? 0) > 0)
            ColourList = colourList;

      }

      /// <summary>
      /// Retrieves the color at index specified by the input parameter in a usable format.
      /// </summary>
      /// <param name="colourIndex">Index within the IfcColourRgbList.  This should be in the range [1,n] (n = number of items in IFCColourRgbList).</param>
      /// <returns>Color representing the colour at the index.</returns>
      public Color GetColor(int colourIndex)
      {
         Color color = null;
         int count = ColourList?.Count ?? 0;
         if (colourIndex > 0 && colourIndex <= count)
         {
            IList<double> colorAtIndex = ColourList[colourIndex-1];
            byte red = (byte)(Math.Round(colorAtIndex[(int)ColorComponent.Red] * 255));
            byte green = (byte)(Math.Round(colorAtIndex[(int)ColorComponent.Green] * 255));
            byte blue = (byte)(Math.Round(colorAtIndex[(int)ColorComponent.Blue] * 255));
            color = new Color(red, green, blue);
         }

         return color;
      }

      /// <summary>
      /// Start processing the IFCColourRgbList
      /// </summary>
      /// <param name="ifcColourRgbList">the handle</param>
      /// <returns></returns>
      public static IFCColourRgbList ProcessIFCColourRgbList (IFCAnyHandle ifcColourRgbList)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue (ifcColourRgbList))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcColourRgbList);
            return null;
         }

         IFCEntity colourRgbList;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcColourRgbList.StepId, out colourRgbList))
            colourRgbList = new IFCColourRgbList(ifcColourRgbList);

         return (colourRgbList as IFCColourRgbList);
      }
   }
}
