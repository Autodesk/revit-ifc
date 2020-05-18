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
   /// <summary>
   /// Class that represents IFCBSplineCurveWithKnots entity
   /// </summary>
   public class IFCBSplineCurveWithKnots : IFCBSplineCurve
   {

      private IList<int> m_KnotMultiplicities;

      /// <summary>
      /// The multiplicities of the knots. This list defines the number of times each knot in the knots list is to be repeated in constructing the knot array
      /// </summary>
      public IList<int> KnotMultiplicities
      {
         get { return m_KnotMultiplicities; }
         set { m_KnotMultiplicities = value; }
      }

      private IList<double> m_Knots;

      /// <summary>
      /// The list of distinct knots used to define the B-spline basis functions.
      /// </summary>
      public IList<double> Knots
      {
         get { return m_Knots; }
         set { m_Knots = value; }
      }

      protected IFCBSplineCurveWithKnots()
      {
      }

      protected IFCBSplineCurveWithKnots(IFCAnyHandle bSplineCurve)
      {
         Process(bSplineCurve);
      }

      protected override void Process(IFCAnyHandle ifcCurve)
      {
         base.Process(ifcCurve);
         KnotMultiplicities = IFCAnyHandleUtil.GetAggregateIntAttribute<List<int>>(ifcCurve, "KnotMultiplicities");
         Knots = IFCAnyHandleUtil.GetAggregateDoubleAttribute<List<double>>(ifcCurve, "Knots");

         if (KnotMultiplicities == null || Knots == null)
         {
            Importer.TheLog.LogError(ifcCurve.StepId, "Cannot find the KnotMultiplicities or Knots attribute of this IfcBSplineCurveWithKnots", true);
         }

         if (KnotMultiplicities.Count != Knots.Count)
         {
            Importer.TheLog.LogError(ifcCurve.StepId, "The number of knots and knot multiplicities are not the same", true);
         }

         IList<double> revitKnots = IFCGeometryUtil.ConvertIFCKnotsToRevitKnots(KnotMultiplicities, Knots);

         Curve nurbsSpline = NurbSpline.CreateCurve(Degree, revitKnots, ControlPointsList);
         SetCurve(nurbsSpline);

         if (nurbsSpline == null)
         {
            Importer.TheLog.LogWarning(ifcCurve.StepId, "Cannot get the curve representation of this IfcCurve", false);
         }
      }

      /// <summary>
      /// Creates an IFCBSplineCurveWithKnots from a handle of type IfcBSplineCurveWithKnots
      /// </summary>
      /// <param name="ifcBSplineCurve">The handle</param>
      /// <returns>The IFCBSplineCurveWithKnots object</returns>
      public static IFCBSplineCurveWithKnots ProcessIFCBSplineCurveWithKnots(IFCAnyHandle ifcBSplineCurve)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcBSplineCurve))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcBSplineCurveWithKnots);
            return null;
         }

         IFCEntity bSplineCurve = null;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcBSplineCurve.StepId, out bSplineCurve))
            bSplineCurve = new IFCBSplineCurveWithKnots(ifcBSplineCurve);

         return (bSplineCurve as IFCBSplineCurveWithKnots);
      }

      protected bool constraintsParamBSpline()
      {
         // TODO: implement this function to validate NURBS data
         //       implementation can be found here http://www.buildingsmart-tech.org/ifc/IFC4/final/html/schema/ifcgeometryresource/lexical/ifcconstraintsparambspline.htm
         //       move this function to the correct place
         return true;
      }
   }
}