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
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Represents an IfcFeatureElementSubtraction.
   /// </summary>
   public class IFCFeatureElementSubtraction : IFCFeatureElement
   {
      protected IFCElement m_VoidsElement = null;

      /// <summary>
      /// The element this opening is voiding (e.g., a wall).
      /// This is set initially by the host object.
      /// </summary>
      public IFCElement VoidsElement
      {
         get { return m_VoidsElement; }
         set { m_VoidsElement = value; }
      }

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCFeatureElementSubtraction()
      {

      }

      /// <summary>
      /// Constructs an IFCFeatureElementSubtraction from the IfcFeatureElementSubtraction handle.
      /// </summary>
      /// <param name="ifcFeatureElementSubtraction">The IfcFeatureElementSubtraction handle.</param>
      protected IFCFeatureElementSubtraction(IFCAnyHandle ifcFeatureElementSubtraction)
      {
         Process(ifcFeatureElementSubtraction);
      }

      /// <summary>
      /// Processes IfcFeatureElementSubtraction attributes.
      /// </summary>
      /// <param name="ifcFeatureElementSubtraction">The IfcFeatureElementSubtraction handle.</param>
      protected override void Process(IFCAnyHandle ifcFeatureElementSubtraction)
      {
         base.Process(ifcFeatureElementSubtraction);
      }

      /// <summary>
      /// Processes an IfcFeatureElementSubtraction object.
      /// </summary>
      /// <param name="ifcFeatureElementSubtraction">The IfcFeatureElementSubtraction handle.</param>
      /// <returns>The iFCFeatureElementSubtraction object.</returns>
      public static IFCFeatureElementSubtraction ProcessIFCFeatureElementSubtraction(IFCAnyHandle ifcFeatureElementSubtraction)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcFeatureElementSubtraction))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcFeatureElementSubtraction);
            return null;
         }

         IFCEntity cachedFeatureElementSubtraction;
         if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcFeatureElementSubtraction.StepId, out cachedFeatureElementSubtraction))
            return (cachedFeatureElementSubtraction as IFCFeatureElementSubtraction);

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcFeatureElementSubtraction, IFCEntityType.IfcOpeningElement))
            return IFCOpeningElement.ProcessIFCOpeningElement(ifcFeatureElementSubtraction);

         return new IFCFeatureElementSubtraction(ifcFeatureElementSubtraction);
      }
   }
}