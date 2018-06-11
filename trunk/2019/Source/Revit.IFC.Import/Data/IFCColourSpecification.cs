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
   /// Class representing the abstract IfcColourSpecification entity.
   /// </summary>
   public abstract class IFCColourSpecification : IFCEntity
   {
      private string m_Name = null;

      // cached color value.
      private Color m_Color = null;

      protected IFCColourSpecification()
      {
      }

      protected abstract Color CreateColor();

      /// <summary>
      /// Get the optional name of the color.
      /// </summary>
      public string Name
      {
         get { return m_Name; }
         protected set { m_Name = value; }
      }

      /// <summary>
      /// Get the RGB associated to the color.
      /// </summary>
      /// <returns>The Color value.</returns>
      public Color GetColor()
      {
         if (m_Color == null)
            m_Color = CreateColor();

         return m_Color;
      }

      override protected void Process(IFCAnyHandle item)
      {
         base.Process(item);

         Name = IFCImportHandleUtil.GetOptionalStringAttribute(item, "Name", null);
      }

      protected IFCColourSpecification(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Processes an IfcColourSpecification entity handle.
      /// </summary>
      /// <param name="ifcColourSpecification">The IfcColourSpecification handle.</param>
      /// <returns>The IFCColourSpecification object.</returns>
      public static IFCColourSpecification ProcessIFCColourSpecification(IFCAnyHandle ifcColourSpecification)
      {
         if (IFCAnyHandleUtil.IsSubTypeOf(ifcColourSpecification, IFCEntityType.IfcColourRgb))
            return IFCColourRgb.ProcessIFCColourRgb(ifcColourSpecification);

         Importer.TheLog.LogUnhandledSubTypeError(ifcColourSpecification, IFCEntityType.IfcColourSpecification, true);
         return null;
      }
   }
}