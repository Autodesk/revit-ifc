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
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCGeometricSet : IFCRepresentationItem
   {
      IList<IFCCurve> m_Curves = null;

      /// <summary>
      /// Get the Curve representation of IFCCurve.  It could be null.
      /// </summary>
      public IList<IFCCurve> Curves
      {
         get
         {
            if (m_Curves == null)
               m_Curves = new List<IFCCurve>();
            return m_Curves;
         }
      }

      protected IFCGeometricSet()
      {
      }

      override protected void Process(IFCAnyHandle ifcGeometricSet)
      {
         base.Process(ifcGeometricSet);

         IList<IFCAnyHandle> elements = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcGeometricSet, "Elements");
         if (elements != null)
         {
            foreach (IFCAnyHandle element in elements)
            {
               if (IFCAnyHandleUtil.IsSubTypeOf(element, IFCEntityType.IfcCurve))
               {
                  IFCCurve curve = IFCCurve.ProcessIFCCurve(element);
                  if (curve != null)
                     Curves.Add(curve);
               }
               else
                  Importer.TheLog.LogError(Id, "Unhandled entity type in IfcGeometricSet: " + IFCAnyHandleUtil.GetEntityType(element).ToString(), false);
            }
         }
      }

      protected IFCGeometricSet(IFCAnyHandle geometricSet)
      {
         Process(geometricSet);
      }

      /// <summary>
      /// Create an IFCGeometricSet object from a handle of type IfcGeometricSet.
      /// </summary>
      /// <param name="ifcGeometricSet">The IFC handle.</param>
      /// <returns>The IFCGeometricSet object.</returns>
      public static IFCGeometricSet ProcessIFCGeometricSet(IFCAnyHandle ifcGeometricSet)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcGeometricSet))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcGeometricSet);
            return null;
         }

         IFCEntity geometricSet;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcGeometricSet.StepId, out geometricSet))
            geometricSet = new IFCGeometricSet(ifcGeometricSet);
         return (geometricSet as IFCGeometricSet);
      }

      /// <summary>
      /// Create geometry for a particular representation item, and add to scope.
      /// </summary>
      /// <param name="shapeEditScope">The geometry creation scope.</param>
      /// <param name="lcs">Local coordinate system for the geometry, without scale.</param>
      /// <param name="scaledLcs">Local coordinate system for the geometry, including scale, potentially non-uniform.</param>
      /// <param name="guid">The guid of an element for which represntation is being created.</param>
      /// <remarks>This currently assumes that we are creating plan view curves.</remarks>
      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, guid);

         foreach (IFCCurve curve in Curves)
         {
            curve.CreateShape(shapeEditScope, lcs, scaledLcs, guid);
         }
      }
   }
}