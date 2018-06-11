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
   /// Class that represents IFCRationalBSplineSurfaceWithKnots entity
   /// </summary>
   public class IFCRationalBSplineSurfaceWithKnots : IFCBSplineSurfaceWithKnots
   {
      /// <summary>
      /// The list of weights.  We use a List instead of an IList to allow use of the AddRange function.
      /// </summary>
      private List<double> m_WeightsList;

      /// <summary>
      /// The list of weights
      /// </summary>
      /// <remarks>
      /// Based on IFC 4 specification, the weights are represented by a list of lists, with each list being one 
      /// row of u-values. We convert it to one list of control points by appending all of these lists together
      /// in order, i.e. the first list is followed by the second list and so on.
      /// </remarks>
      public List<double> WeightsList
      {
         get
         {
            if (m_WeightsList == null)
               m_WeightsList = new List<double>();
            return m_WeightsList;
         }
         protected set { m_WeightsList = value; }
      }

      protected IFCRationalBSplineSurfaceWithKnots()
      {
      }

      protected IFCRationalBSplineSurfaceWithKnots(IFCAnyHandle ifcRationalBSplineSurfaceWithKnots)
      {
         Process(ifcRationalBSplineSurfaceWithKnots);
      }

      protected override void Process(IFCAnyHandle ifcRationalBSplineSurfaceWithKnots)
      {
         base.Process(ifcRationalBSplineSurfaceWithKnots);

         IList<IList<double>> weightsData = IFCImportHandleUtil.GetListOfListOfDoubleAttribute(ifcRationalBSplineSurfaceWithKnots, "WeightsData");
         if (weightsData != null)
         {
            foreach (IList<double> weightsRow in weightsData)
            {
               WeightsList.AddRange(weightsRow);
            }
         }
      }

      /// <summary>
      /// Create an IFCRationalBSplineSurfaceWithKnots object from the handle of type IfcRationalBSplineSurfaceWithKnots
      /// </summary>
      /// <param name="IFCRationalBSplineSurfaceWithKnots">The IFC handle</param>
      /// <returns>The IFCRationalBSplineSurfaceWithKnots object</returns>
      public static IFCRationalBSplineSurfaceWithKnots ProcessIFCRationalBSplineSurfaceWithKnots(IFCAnyHandle ifcRationalBSplineSurfaceWithKnots)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcRationalBSplineSurfaceWithKnots))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcRationalBSplineSurfaceWithKnots);
            return null;
         }

         IFCEntity rationalBSplineSurfaceWithKnots = null;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcRationalBSplineSurfaceWithKnots.StepId, out rationalBSplineSurfaceWithKnots))
            rationalBSplineSurfaceWithKnots = new IFCRationalBSplineSurfaceWithKnots(ifcRationalBSplineSurfaceWithKnots);

         return (rationalBSplineSurfaceWithKnots as IFCRationalBSplineSurfaceWithKnots);
      }
   }
}