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
   /// Represents an IfcProxy.
   /// </summary>
   public class IFCProxy : IFCProduct
   {
      private string m_Tag = null;

      private string m_ProxyType = null;

      /// <summary>
      /// The "Tag" field associated with the IfcProxy.
      /// </summary>
      public string Tag
      {
         get { return m_Tag; }
         protected set { m_Tag = value; }
      }

      /// <summary>
      /// The "ProxyType" field associated with the IfcProxy.
      /// </summary>
      public string ProxyType
      {
         get { return m_ProxyType; }
         protected set { m_ProxyType = value; }
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCProxy()
      {

      }

      /// <summary>
      /// Constructs an IFCProxy from the IfcProxy handle.
      /// </summary>
      /// <param name="ifcProxy">The IfcProxy handle.</param>
      protected IFCProxy(IFCAnyHandle ifcProxy)
      {
         Process(ifcProxy);
      }

      /// <summary>
      /// Processes IfcProxy attributes.
      /// </summary>
      /// <param name="ifcProxy">The IfcProxy handle.</param>
      protected override void Process(IFCAnyHandle ifcProxy)
      {
         base.Process(ifcProxy);

         Tag = IFCAnyHandleUtil.GetStringAttribute(ifcProxy, "Tag");

         ProxyType = IFCAnyHandleUtil.GetEnumerationAttribute(ifcProxy, "ProxyType");
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
            // Set "Tag" parameter.
            string ifcTag = Tag;
            if (!string.IsNullOrWhiteSpace(ifcTag))
               IFCPropertySet.AddParameterString(doc, element, "IfcTag", ifcTag, Id);

            // Set "ProxyType" parameter.
            string ifcProxyType = ProxyType;
            if (!string.IsNullOrWhiteSpace(ifcProxyType))
               IFCPropertySet.AddParameterString(doc, element, "IfcProxyType", ifcProxyType, Id);
         }
      }

      /// <summary>
      /// Processes an IfcProxy object.
      /// </summary>
      /// <param name="ifcProxy">The IfcProxy handle.</param>
      /// <returns>The IFCProxy object.</returns>
      public static IFCProxy ProcessIFCProxy(IFCAnyHandle ifcProxy)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcProxy))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcProxy);
            return null;
         }

         IFCEntity cachedIFCProxy;
         IFCImportFile.TheFile.EntityMap.TryGetValue(ifcProxy.StepId, out cachedIFCProxy);
         if (cachedIFCProxy != null)
            return (cachedIFCProxy as IFCProxy);

         return new IFCProxy(ifcProxy);
      }
   }
}