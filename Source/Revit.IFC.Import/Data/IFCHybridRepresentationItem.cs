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
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   // For Hybrid IFC Import, Revit needs something to represent Body Geometry other than null.
   // Body geometry for DirectShapes are actually created by AnyCAD, but there are other data within a RepresentationItem
   // that must persist.
   // IFCHybridRepresentationItem must inherit from IIFCBooleanOperand, since it may need to represent an operand, which (in essence)
   // has already been created.
   public class IFCHybridRepresentationItem : IFCRepresentationItem, IIFCBooleanOperand
   {
      protected IFCHybridRepresentationItem()
      {
      }

      /// <summary>
      /// Constructor to create a new IFCHybridRepresentationItem.
      /// </summary>
      /// <param name="ifcRepresentationItem">Handle representing IFCRepresentationItem.</param>
      protected IFCHybridRepresentationItem(IFCAnyHandle ifcRepresentationItem)
      {
         Process(ifcRepresentationItem);
      }

      /// <summary>
      /// Process IFCHybridRepresentationItem members.
      /// Even though we don't have any members, it exists to maintain a parallel structure of other IFCEntity processing.
      /// </summary>
      /// <param name="ifcRepresentationItem">Handle representing IFCRepresentationItem.</param>
      override protected void Process(IFCAnyHandle ifcRepresentationItem)
      {
         base.Process(ifcRepresentationItem);
      }

      /// <summary>
      /// Create the IFCHybridRepresentationItem.
      /// </summary>
      /// <param name="ifcRepresentationItem">Handle corresponding to the IFCRepresentationItem that AnyCAD already processed.</param>
      /// <returns>IFCHybridRepresentationItem object.</returns>
      public static IFCHybridRepresentationItem ProcessIFCHybridRepresentationItem(IFCAnyHandle ifcRepresentationItem)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcRepresentationItem))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcRepresentationItem);
            return null;
         }

         IFCEntity hybridRepresentationItem;
         IFCImportFile.TheFile.EntityMap.TryGetValue(ifcRepresentationItem.StepId, out hybridRepresentationItem);
         if (hybridRepresentationItem != null)
            return (hybridRepresentationItem as IFCHybridRepresentationItem);

         return new IFCHybridRepresentationItem(ifcRepresentationItem);
      }

      /// <summary>
      /// Return geometry for a particular representation item.
      /// In the case of Hybrid, Geometry has already been created, so only process the IfcStyledItem.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      /// <returns>Zero or more created Solids.</returns>
      public IList<GeometryObject> CreateGeometry(IFCImportShapeEditScope shapeEditScope, Transform scaledLcs, string guid)
      {
         if (StyledByItem != null)
            StyledByItem.Create(shapeEditScope);

         return null;
      }

      /// <summary>
      /// In case of a Boolean operation failure, provide a recommended direction to shift the geometry in for a second attempt.
      /// </summary>
      /// <param name="lcs">The local transform for this entity.</param>
      /// <returns>An XYZ representing a unit direction vector, or null if no direction is suggested.</returns>
      /// <remarks>If the 2nd attempt fails, a third attempt will be done with a shift in the opposite direction.</remarks>
      public XYZ GetSuggestedShiftDirection(Transform lcs)
      {
         return null;
      }

   }
}
