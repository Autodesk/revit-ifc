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

using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCDerivedProfileDef : IFCProfileDef
   {
      private IFCProfileDef m_ParentProfile;

      private IFCCartesianTransformOperator m_Operator;

      private string m_Label;

      protected IFCDerivedProfileDef()
      {
      }

      protected IFCDerivedProfileDef(IFCAnyHandle ifcDerivedProfileDef)
      {
         Process(ifcDerivedProfileDef);
      }

      /// <summary>
      /// Get the base profile definition.
      /// </summary>
      public IFCProfileDef ParentProfile
      {
         get { return m_ParentProfile; }
         protected set { m_ParentProfile = value; }
      }

      /// <summary>
      /// Get the transform of the base profile that defines this derived profile.
      /// </summary>
      public IFCCartesianTransformOperator Operator
      {
         get { return m_Operator; }
         protected set { m_Operator = value; }
      }

      /// <summary>
      /// Get the label associated with the profile.
      /// </summary>
      public string Label
      {
         get { return m_Label; }
         protected set { m_Label = value; }
      }

      override protected void Process(IFCAnyHandle ifcDerivedProfileDef)
      {
         base.Process(ifcDerivedProfileDef);

         IFCAnyHandle ifcParentProfile = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcDerivedProfileDef, "ParentProfile", true);
         ParentProfile = IFCProfileDef.ProcessIFCProfileDef(ifcParentProfile);

         IFCAnyHandle ifcOperator = IFCImportHandleUtil.GetRequiredInstanceAttribute(ifcDerivedProfileDef, "Operator", true);
         Operator = IFCCartesianTransformOperator.ProcessIFCCartesianTransformOperator(ifcOperator);

         Label = IFCAnyHandleUtil.GetStringAttribute(ifcDerivedProfileDef, "Label");
      }

      /// <summary>
      /// Create an IFCDerivedProfileDef object from a handle of type IfcDerivedProfileDef.
      /// </summary>
      /// <param name="ifcDerivedProfileDef">The IFC handle.</param>
      /// <returns>The IFCDerivedProfileDef object.</returns>
      public static IFCDerivedProfileDef ProcessIFCDerivedProfileDef(IFCAnyHandle ifcDerivedProfileDef)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcDerivedProfileDef))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcDerivedProfileDef);
            return null;
         }

         IFCEntity derivedProfileDef;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcDerivedProfileDef.StepId, out derivedProfileDef))
            derivedProfileDef = new IFCDerivedProfileDef(ifcDerivedProfileDef);
         return (derivedProfileDef as IFCDerivedProfileDef);
      }
   }
}