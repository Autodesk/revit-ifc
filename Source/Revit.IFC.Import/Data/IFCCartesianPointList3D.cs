using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   /// <summary>
   /// Class that represents an IfcCartesianPointList.
   /// </summary>
   /// <remarks>This can be either a IfcCartesianPointList2D or a IfcCartesianPoint3D.
   /// Both will be converted to XYZ values.</remarks>
   public class IFCCartesianPointList : IFCRepresentationItem
   {
      IList<XYZ> m_CoordList = null;

      protected IFCCartesianPointList()
      {
      }

      /// <summary>
      /// The list of vertices, where the vertices are represented as an IList of doubles.
      /// </summary>
      public IList<XYZ> CoordList
      {
         get { return m_CoordList; }
         protected set { m_CoordList = value; }
      }

      /// <summary>
      /// Create IFCCartesianPointList instance
      /// </summary>
      /// <param name="item">The handle</param>
      protected IFCCartesianPointList(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Process the IfcCartesianPointList handle.
      /// </summary>
      /// <param name="item">The handle</param>
      protected override void Process(IFCAnyHandle item)
      {
         base.Process(item);

         CoordList = new List<XYZ>();

         IList<IList<double>> coordList = IFCImportHandleUtil.GetListOfListOfDoubleAttribute(item, "CoordList");
         if (coordList != null)
         {
            foreach (IList<double> coord in coordList)
            {
               // TODO: we expect size to be 2 or 3.  Warn if not?
               if (coord == null)
                  continue;

               int size = coord.Count;
               CoordList.Add(new XYZ(
                  (size > 0 ? IFCUnitUtil.ScaleLength(coord[0]) : 0.0),
                  (size > 1 ? IFCUnitUtil.ScaleLength(coord[1]) : 0.0),
                  (size > 2 ? IFCUnitUtil.ScaleLength(coord[2]) : 0.0)));
            }
         }
      }

      /// <summary>
      /// Accept the handle for IFCCartesianPointList and return the instance (creating it if not yet created)
      /// </summary>
      /// <param name="ifcCartesianPointList">The handle.</param>
      /// <returns>The associated IFCCartesianPointList class.</returns>
      public static IFCCartesianPointList ProcessIFCCartesianPointList(IFCAnyHandle ifcCartesianPointList)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcCartesianPointList))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcCartesianPointList);
            return null;
         }

         IFCEntity cartesianPointList;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcCartesianPointList.StepId, out cartesianPointList))
            cartesianPointList = new IFCCartesianPointList(ifcCartesianPointList);
         return (cartesianPointList as IFCCartesianPointList);
      }
   }
}