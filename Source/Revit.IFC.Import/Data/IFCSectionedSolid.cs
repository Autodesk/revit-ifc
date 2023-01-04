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
   /// An IfcSectionedSolid is an abstract base type for solids constructed by sweeping potentially
   /// variable cross sections along a directrix.
   /// 
   /// This is very similar to the IFCSweptAreaSolid / IFCSurfaceCurveSweptAreaSolid combination.
   /// </summary>
   public abstract class IFCSectionedSolid : IFCSolidModel
   {
      /// <summary>
      /// The curve used to define the sweeping operation.
      /// </summary>
      public IFCCurve Directrix { get; set; } = null;

      /// <summary>
      /// List of cross sections in sequential order along the Directrix.
      /// Should have at least two.
      /// </summary>
      public IList<IFCProfileDef> CrossSections { get; set; } = null;


      /// <summary>
      /// Default Constructor.
      /// </summary>
      protected IFCSectionedSolid ()
      {
      }

      
      /// <summary>
      /// Process Attributes for IFCSectionedSolid.
      /// </summary>
      /// <param name="solid">Handle for the IFCSectionedSolid.</param>
      override protected void Process(IFCAnyHandle solid)
      {
         base.Process(solid);

         IFCAnyHandle directrix = IFCImportHandleUtil.GetRequiredInstanceAttribute(solid, "Directrix", true);
         Directrix = IFCCurve.ProcessIFCCurve(directrix);

         IList<IFCAnyHandle> crossSections = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(solid, "CrossSections");
         if (crossSections?.Count > 2)
         {
            CrossSections = new List<IFCProfileDef>();
            foreach (IFCAnyHandle crossSection in crossSections)
            {
               CrossSections.Add(IFCProfileDef.ProcessIFCProfileDef(crossSection));
            }
         }
      }


      /// <summary>
      /// Constructor to process attributes for IFCSectionedSolid.
      /// </summary>
      /// <param name="solid">Handle representing solid.</param>
      protected IFCSectionedSolid(IFCAnyHandle solid)
      {
         Process(solid);
      }


      /// <summary>
      /// GetTransformedCurveLoops to create Geometry.
      /// Currently only support IFCSimpleProfile.
      /// </summary>
      /// <param name="unscaledLcs">The unscaled transform, if the scaled transform isn't supported.</param>
      /// <param name="scaledLcs">The scaled (true) transform.</param>
      /// <returns>List of list of CurveLoops representing profiles of exactly one outer and zero or more inner loops.</returns>
      protected IList<IList<CurveLoop>> GetTransformedCurveLoops(Transform scaledLcs)
      {
         IList<IList<CurveLoop>> listOfListOfLoops = new List<IList<CurveLoop>>();

         foreach (IFCProfileDef crossSection in CrossSections)
         {
            IFCSimpleProfile simpleProfile = crossSection as IFCSimpleProfile;
            if (simpleProfile == null)
            {
               // TODO:  Support more complex ProfileDefs.
               //
               Importer.TheLog.LogError(Id, "Cross Section Profile #" + crossSection.Id + "Not yet supported.", false);
               continue;
            }

            IList<CurveLoop> listOfLoops = new List<CurveLoop>();

            // It is legal for simpleSweptArea.Position to be null, for example for IfcArbitraryClosedProfileDef.
            Transform scaledSweptAreaPosition = (simpleProfile.Position == null) ? scaledLcs : scaledLcs.Multiply(simpleProfile.Position);

            CurveLoop currLoop = simpleProfile.GetTheOuterCurveLoop();
            if (currLoop?.Count() == 0)
            {
               Importer.TheLog.LogError(simpleProfile.Id, "No outer curve loop for profile, ignoring.", false);
               continue;
            }

            currLoop = IFCGeometryUtil.SplitUnboundCyclicCurves(currLoop);
            listOfLoops.Add(IFCGeometryUtil.CreateTransformed(currLoop, Id, scaledSweptAreaPosition));

            if (simpleProfile.InnerCurves != null)
            {
               foreach (CurveLoop innerCurveLoop in simpleProfile.InnerCurves)
               {
                  listOfLoops.Add(IFCGeometryUtil.CreateTransformed(IFCGeometryUtil.SplitUnboundCyclicCurves(innerCurveLoop), Id, scaledSweptAreaPosition));
               }
            }

            listOfListOfLoops.Add(listOfLoops);
         }

         return listOfListOfLoops;
      }


      /// <summary>
      /// Creates an IFCSectionedSolid object from supplied handle.
      /// </summary>
      /// <param name="ifcSectionedSolid">Handle that represents an IFCSectionedSolid.</param>
      /// <returns>IFCSectionedSolid object.</returns>
      public static IFCSectionedSolid ProcessIFCSectionedSolid(IFCAnyHandle ifcSectionedSolid)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcSectionedSolid))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcSectionedSolid);
            return null;
         }

         if (IFCAnyHandleUtil.IsSubTypeOf(ifcSectionedSolid, IFCEntityType.IfcSectionedSolidHorizontal))
            return IFCSectionedSolidHorizontal.ProcessIFCSectionedSolidHorizontal(ifcSectionedSolid);

         Importer.TheLog.LogUnhandledSubTypeError(ifcSectionedSolid, IFCEntityType.IfcSectionedSolid, true);
         return null;
      }
   }
}
