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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;

namespace Revit.IFC.Import.Data
{
   public class IFCPresentationStyleAssignment : IFCEntity
   {
      ISet<IFCPresentationStyle> m_Styles = null;

      public ISet<IFCPresentationStyle> Styles
      {
         get
         {
            if (m_Styles == null)
               m_Styles = new HashSet<IFCPresentationStyle>();
            return m_Styles;
         }
      }

      protected IFCPresentationStyleAssignment()
      {
      }

      override protected void Process(IFCAnyHandle item)
      {
         base.Process(item);

         HashSet<IFCData> styles = IFCAnyHandleUtil.GetAggregateAttribute<HashSet<IFCData>>(item, "Styles");
         if (styles != null)
         {
            foreach (IFCData styleData in styles)
            {
               if (styleData.PrimitiveType.ToString() == "Instance")
               {
                  IFCAnyHandle style = styleData.AsInstance();
                  try
                  {
                     IFCPresentationStyle presentationStyle = IFCPresentationStyle.ProcessIFCPresentationStyle(style);
                     if (presentationStyle != null)
                        Styles.Add(presentationStyle);
                  }
                  catch (Exception ex)
                  {
                     Importer.TheLog.LogError(item.StepId, ex.Message, false);
                  }
               }
            }
         }
      }

      protected IFCPresentationStyleAssignment(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Processes an IfcPresentationStyleAssignment entity handle.
      /// </summary>
      /// <param name="ifcPresentationStyleAssignment">The IfcPresentationStyleAssignment handle.</param>
      /// <returns>The IFCPresentationStyleAssignment object.</returns>
      public static IFCPresentationStyleAssignment ProcessIFCPresentationStyleAssignment(IFCAnyHandle ifcPresentationStyleAssignment)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPresentationStyleAssignment))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcPresentationStyleAssignment);
            return null;
         }

         IFCEntity presentationStyleAssignment;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcPresentationStyleAssignment.StepId, out presentationStyleAssignment))
            presentationStyleAssignment = new IFCPresentationStyleAssignment(ifcPresentationStyleAssignment);
         return (presentationStyleAssignment as IFCPresentationStyleAssignment);
      }
   }
}