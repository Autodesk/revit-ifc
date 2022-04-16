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

namespace Revit.IFC.Import.Data
{
   public class IFCCSGSolid : IFCSolidModel
   {
      public IFCBooleanResult TreeRootExpression { get; protected set; } = null;
      
      protected IFCCSGSolid()
      {
      }

      protected override IList<GeometryObject> CreateGeometryInternal(
         IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         if (TreeRootExpression != null)
            return TreeRootExpression.CreateGeometry(shapeEditScope, lcs, scaledLcs, guid);
         return null;
      }

      /// <summary>
      /// Create geometry for a particular representation item.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, guid);

         IList<GeometryObject> csgGeometries = CreateGeometryInternal(shapeEditScope, lcs, scaledLcs, guid);
         if (csgGeometries != null)
         {
            foreach (GeometryObject csgGeometry in csgGeometries)
            {
               shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, csgGeometry));
            }
         }
      }

      override protected void Process(IFCAnyHandle solid)
      {
         base.Process(solid);

         IFCAnyHandle treeRootExpression = IFCImportHandleUtil.GetRequiredInstanceAttribute(solid, "TreeRootExpression", false);
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(treeRootExpression))
         {
            if (IFCAnyHandleUtil.IsValidSubTypeOf(treeRootExpression, IFCEntityType.IfcBooleanResult))
               TreeRootExpression = IFCBooleanResult.ProcessIFCBooleanResult(treeRootExpression);
            else
               Importer.TheLog.LogUnhandledSubTypeError(treeRootExpression, "IfcCsgSelect", false);
         }
      }

      protected IFCCSGSolid(IFCAnyHandle solid)
      {
         Process(solid);
      }

      /// <summary>
      /// Create an IFCCSGSolid object from a handle of type IfcCSGSolid.
      /// </summary>
      /// <param name="ifcSweptAreaSolid">The IFC handle.</param>
      /// <returns>The IFCCSGSolid object.</returns>
      public static IFCCSGSolid ProcessIFCCSGSolid(IFCAnyHandle ifcCSGSolid)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcCSGSolid))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcCsgSolid);
            return null;
         }

         IFCEntity csgSolid;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcCSGSolid.StepId, out csgSolid))
            csgSolid = new IFCCSGSolid(ifcCSGSolid);

         return (csgSolid as IFCCSGSolid);
      }
   }
}