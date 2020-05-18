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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;
using System.Collections.Generic;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class that represents IFCTrimmedCurve entity
   /// </summary>
   public class IFCTrimmedCurve : IFCBoundedCurve
   {
      private double? m_Trim1Parameter = 0.0;

      private double? m_Trim2Parameter = 0.0;

      /// <summary>
      /// The start trim parameter of the IFCTrimmedCurve.  This is the unprocessed double parameter value.
      /// </summary>
      public double? Trim1
      {
         get { return m_Trim1Parameter; }
         protected set { m_Trim1Parameter = value; }
      }

      /// <summary>
      /// The end trim parameter of the IFCTrimmedCurve.  This is the unprocessed double parameter value.
      /// </summary>
      public double? Trim2
      {
         get { return m_Trim2Parameter; }
         protected set { m_Trim2Parameter = value; }
      }

      protected IFCTrimmedCurve()
      {
      }

      protected IFCTrimmedCurve(IFCAnyHandle trimmedCurve)
      {
         Process(trimmedCurve);
      }

      private bool NeedToReverseBaseCurve(Curve baseCurve, 
         double param1, double param2, IFCTrimmingPreference trimPreference)
      {
         // In the very specific case where the trim preference is Cartesian,
         // and the trim parameters are reversed, we can try again by reversing
         // the base curve, if it is not cyclic.  
         // This is an error on the input that we see in some files.
         return (baseCurve != null &&
            param1 > param2 + MathUtil.Eps() &&
            trimPreference == IFCTrimmingPreference.Cartesian &&
            !baseCurve.IsCyclic);
      }

      private bool SafelyBoundCurve(Curve curve, double param1, double param2)
      {
         if (curve == null)
            return false;

         try
         {
            curve.MakeBound(param1, param2);
            return true;
         }
         catch
         {
            BackupCurveStartLocation = curve.Evaluate(param1, false);
            BackupCurveEndLocation = curve.Evaluate(param2, false);
         }

         return false;
      }

      protected override void Process(IFCAnyHandle ifcCurve)
      {
         base.Process(ifcCurve);

         bool found = false;

         bool sameSense = IFCImportHandleUtil.GetRequiredBooleanAttribute(ifcCurve, "SenseAgreement", out found);
         if (!found)
            sameSense = true;

         IFCAnyHandle basisCurve = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcCurve, "BasisCurve", true);
         IFCCurve ifcBasisCurve = IFCCurve.ProcessIFCCurve(basisCurve);
         if (ifcBasisCurve == null || (ifcBasisCurve.IsEmpty()))
         {
            // LOG: ERROR: Error processing BasisCurve # for IfcTrimmedCurve #.
            return;
         }
         if (ifcBasisCurve.Curve == null)
         {
            // LOG: ERROR: Expected a single curve, not a curve loop for BasisCurve # for IfcTrimmedCurve #.
            return;
         }

         IFCData trim1 = ifcCurve.GetAttribute("Trim1");
         if (trim1.PrimitiveType != IFCDataPrimitiveType.Aggregate)
         {
            // LOG: ERROR: Invalid data type for Trim1 attribute for IfcTrimmedCurve #.
            return;
         }

         IFCData trim2 = ifcCurve.GetAttribute("Trim2");
         if (trim2.PrimitiveType != IFCDataPrimitiveType.Aggregate)
         {
            // LOG: ERROR: Invalid data type for Trim1 attribute for IfcTrimmedCurve #.
            return;
         }

         // Note that these are the "unprocessed" values.  These can be used for, e.g., adding up the IFC parameter length
         // of the file, to account for export errors.  The "processed" values can be determined from the Revit curves.
         Trim1 = GetRawTrimParameter(trim1);
         Trim2 = GetRawTrimParameter(trim2);

         IFCTrimmingPreference trimPreference = IFCEnums.GetSafeEnumerationAttribute<IFCTrimmingPreference>(ifcCurve, "MasterRepresentation", IFCTrimmingPreference.Parameter);

         double param1 = 0.0, param2 = 0.0;
         Curve baseCurve = ifcBasisCurve.Curve;
         try
         {
            GetTrimParameters(ifcBasisCurve.Id, trim1, trim2, baseCurve, trimPreference, out param1, out param2);
            if (NeedToReverseBaseCurve(baseCurve, param1, param2, trimPreference))
            {
               Importer.TheLog.LogWarning(Id, "Invalid Param1 > Param2 for non-cyclic IfcTrimmedCurve using Cartesian trimming preference, reversing.", false);
               baseCurve = baseCurve.CreateReversed();
               GetTrimParameters(ifcBasisCurve.Id, trim1, trim2, baseCurve, trimPreference, out param1, out param2);
            }
         }
         catch (Exception ex)
         {
            Importer.TheLog.LogError(ifcCurve.StepId, ex.Message, false);
            return;
         }

         if (MathUtil.IsAlmostEqual(param1, param2))
         {
            Importer.TheLog.LogError(Id, "Param1 = Param2 for IfcTrimmedCurve #, ignoring.", false);
            return;
         }

         Curve curve = null;
         if (baseCurve.IsCyclic)
         {
            double period = baseCurve.Period;
            if (!sameSense)
               MathUtil.Swap(ref param1, ref param2);
            
            // We want to make sure both values are within period of one another.
            param1 = MathUtil.PutInRange(param1, 0, period);
            param2 = MathUtil.PutInRange(param2, 0, period);
            if (param2 < param1)
               param2 = MathUtil.PutInRange(param2, param1 + period/2, period);

            // This is effectively an unbound curve.
            double numberOfPeriods = (param2 - param1) / period;
            if (MathUtil.IsAlmostEqual(numberOfPeriods, Math.Round(numberOfPeriods)))
            {
               Importer.TheLog.LogWarning(Id, "Start and end parameters indicate a zero-length closed curve, assuming unbound is intended.", false);
               curve = baseCurve;
            }
            else
            {
               curve = baseCurve.Clone();
               if (!SafelyBoundCurve(curve, param1, param2))
                  return;
            }
         }
         else
         {
            if (param1 > param2 - MathUtil.Eps())
            {
               Importer.TheLog.LogWarning(Id, "Param1 > Param2 for IfcTrimmedCurve #, reversing.", false);
               MathUtil.Swap(ref param1, ref param2);
               sameSense = !sameSense;
            }

            Curve copyCurve = baseCurve.Clone();

            double length = param2 - param1;
            if (length <= IFCImportFile.TheFile.Document.Application.ShortCurveTolerance)
            {
               string lengthAsString = IFCUnitUtil.FormatLengthAsString(length);
               Importer.TheLog.LogError(Id, "curve length of " + lengthAsString + " is invalid, ignoring.", false);
               return;
            }

            if (!SafelyBoundCurve(copyCurve, param1, param2))
               return;

            if (sameSense)
            {
               curve = copyCurve;
            }
            else
            {
               curve = copyCurve.CreateReversed();
            }
         }

         CurveLoop curveLoop = new CurveLoop();
         curveLoop.Append(curve);
         SetCurveLoop(curveLoop);
      }

      private void GetTrimParameters(int id, IFCData trim1, IFCData trim2, 
         Curve baseCurve, IFCTrimmingPreference trimPreference,
         out double param1, out double param2)
      {
         double? condParam1 = GetTrimParameter(id, trim1, baseCurve, trimPreference, false);
         if (!condParam1.HasValue)
            throw new InvalidOperationException("#" + id + ": Couldn't apply first trimming parameter of IfcTrimmedCurve.");
         param1 = condParam1.Value;

         double? condParam2 = GetTrimParameter(id, trim2, baseCurve, trimPreference, false);
         if (!condParam2.HasValue)
            throw new InvalidOperationException("#" + id + ": Couldn't apply second trimming parameter of IfcTrimmedCurve.");
         param2 = condParam2.Value;

         if (MathUtil.IsAlmostEqual(param1, param2))
         {
            // If we had a cartesian parameter as the trim preference, check if the parameter values are better.
            if (trimPreference == IFCTrimmingPreference.Cartesian)
            {
               condParam1 = GetTrimParameter(id, trim1, baseCurve, IFCTrimmingPreference.Parameter, true);
               if (!condParam1.HasValue)
                  throw new InvalidOperationException("#" + id + ": Couldn't apply first trimming parameter of IfcTrimmedCurve.");
               param1 = condParam1.Value;

               condParam2 = GetTrimParameter(id, trim2, baseCurve, IFCTrimmingPreference.Parameter, true);
               if (!condParam2.HasValue)
                  throw new InvalidOperationException("#" + id + ": Couldn't apply second trimming parameter of IfcTrimmedCurve.");
               param2 = condParam2.Value;
            }
            else
               throw new InvalidOperationException("#" + id + ": Ignoring 0 length curve.");
         }
      }

      private double? GetRawTrimParameter(IFCData trim)
      {
         IFCAggregate trimAggregate = trim.AsAggregate();
         foreach (IFCData trimParam in trimAggregate)
         {
            if (trimParam.PrimitiveType == IFCDataPrimitiveType.Double)
            {
               return trimParam.AsDouble();
            }
         }

         return null;
      }

      private double? GetTrimParameter(int id, IFCData trim, Curve baseCurve, 
         IFCTrimmingPreference trimPreference, bool secondAttempt)
      {
         bool preferParam = !(trimPreference == IFCTrimmingPreference.Cartesian);
         if (secondAttempt)
            preferParam = !preferParam;
         double vertexEps = IFCImportFile.TheFile.Document.Application.VertexTolerance;

         IFCAggregate trimAggregate = trim.AsAggregate();
         foreach (IFCData trimParam in trimAggregate)
         {
            if (!preferParam && (trimParam.PrimitiveType == IFCDataPrimitiveType.Instance))
            {
               IFCAnyHandle trimParamInstance = trimParam.AsInstance();
               XYZ trimParamPt = IFCPoint.ProcessScaledLengthIFCCartesianPoint(trimParamInstance);
               if (trimParamPt == null)
               {
                  Importer.TheLog.LogWarning(id, "Invalid trim point for basis curve.", false);
                  continue;
               }

               try
               {
                  IntersectionResult result = baseCurve.Project(trimParamPt);
                  if (result.Distance < vertexEps)
                     return result.Parameter;

                  Importer.TheLog.LogWarning(id, "Cartesian value for trim point not on the basis curve.", false);
               }
               catch
               {
                  Importer.TheLog.LogWarning(id, "Cartesian value for trim point not on the basis curve.", false);
               }
            }
            else if (preferParam && (trimParam.PrimitiveType == IFCDataPrimitiveType.Double))
            {
               double trimParamDouble = trimParam.AsDouble();
               if (baseCurve.IsCyclic)
                  trimParamDouble = IFCUnitUtil.ScaleAngle(trimParamDouble);
               else
                  trimParamDouble = IFCUnitUtil.ScaleLength(trimParamDouble);
               return trimParamDouble;
            }
         }

         // Try again with opposite preference.
         if (!secondAttempt)
            return GetTrimParameter(id, trim, baseCurve, trimPreference, true);

         return null;
      }

      /// <summary>
      /// Create an IFCTrimmedCurve object from a handle of type IfcTrimmedCurve
      /// </summary>
      /// <param name="ifcTrimmedCurve">The IFC handle</param>
      /// <returns>The IFCTrimmedCurve object</returns>
      public static IFCTrimmedCurve ProcessIFCTrimmedCurve(IFCAnyHandle ifcTrimmedCurve)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcTrimmedCurve))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcTrimmedCurve);
            return null;
         }

         IFCEntity trimmedCurve = null;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcTrimmedCurve.StepId, out trimmedCurve))
            trimmedCurve = new IFCTrimmedCurve(ifcTrimmedCurve);

         return (trimmedCurve as IFCTrimmedCurve);
      }
   }
}