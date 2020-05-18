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
   /// Class that represents IFCBSplineCurve entity
   /// </summary>
   public abstract class IFCBSplineCurve : IFCBoundedCurve
   {
      private int m_Degree;
      private IList<XYZ> m_ControlPointsList;
      private bool? m_ClosedCurve;

      /// <summary>
      /// Indication of whether the curve is closed; it is for information only.
      /// </summary>
      public bool? ClosedCurve
      {
         get { return m_ClosedCurve; }
         protected set { m_ClosedCurve = value; }
      }

      /// <summary>
      /// The algebraic degree of the basis functions.
      /// </summary>
      public int Degree
      {
         get { return m_Degree; }
         protected set
         {
            if (value <= 0)
            {
               throw new InvalidOperationException("Invalid degree");
            }
            else
            {
               m_Degree = value;
            }
         }
      }

      /// <summary>
      /// The list of control points for the curve.
      /// </summary>
      public IList<XYZ> ControlPointsList
      {
         get { return m_ControlPointsList; }
         protected set
         {
            if (value == null || value.Count() <= 1)
            {
               throw new InvalidOperationException("Invalid list of control points");
            }
            else
            {
               m_ControlPointsList = value;
            }
         }
      }

      protected IFCBSplineCurve()
      {
      }

      protected IFCBSplineCurve(IFCAnyHandle bSplineCurve)
      {
         Process(bSplineCurve);
      }

      protected override void Process(IFCAnyHandle ifcCurve)
      {
         base.Process(ifcCurve);

         bool foundDegree = false;
         Degree = IFCImportHandleUtil.GetRequiredIntegerAttribute(ifcCurve, "Degree", out foundDegree);
         if (!foundDegree)
         {
            Importer.TheLog.LogError(ifcCurve.StepId, "Cannot find the degree of this curve", true);
         }

         IList<IFCAnyHandle> controlPoints = IFCAnyHandleUtil.GetValidAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcCurve, "ControlPointsList");

         if (controlPoints == null || controlPoints.Count == 0)
         {
            Importer.TheLog.LogError(ifcCurve.StepId, "This curve has invalid number of control points", true);
         }

         IList<XYZ> controlPointLists = new List<XYZ>();
         foreach (IFCAnyHandle point in controlPoints)
         {
            XYZ pointXYZ = IFCPoint.ProcessScaledLengthIFCCartesianPoint(point);
            controlPointLists.Add(pointXYZ);
         }
         ControlPointsList = controlPointLists;

         bool foundClosedCurve = false;
         IFCLogical closedCurve = IFCImportHandleUtil.GetOptionalLogicalAttribute(ifcCurve, "ClosedCurve", out foundClosedCurve);
         if (!foundClosedCurve)
         {
            Importer.TheLog.LogWarning(ifcCurve.StepId, "Cannot find the ClosedCurve property of this curve, ignoring", false);
            ClosedCurve = null;
         }
         else
         {
            ClosedCurve = (closedCurve == IFCLogical.True);
         }
      }

      /// <summary>
      /// Create an IFCBSplineCurve object from the handle of type IfcBSplineCurve
      /// </summary>
      /// <param name="ifcBSplineCurve">The IFC handle</param>
      /// <returns>The IFCBSplineCurve object</returns>
      public static IFCBSplineCurve ProcessIFCBSplineCurve(IFCAnyHandle ifcBSplineCurve)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcBSplineCurve))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcBSplineCurve);
            return null;
         }

         if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcBSplineCurve, IFCEntityType.IfcBSplineCurveWithKnots))
            return IFCBSplineCurveWithKnots.ProcessIFCBSplineCurveWithKnots(ifcBSplineCurve);

         Importer.TheLog.LogUnhandledSubTypeError(ifcBSplineCurve, IFCEntityType.IfcBSplineCurve, true);
         return null;
      }
   }
}