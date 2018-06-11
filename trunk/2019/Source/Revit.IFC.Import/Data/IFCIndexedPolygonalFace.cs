using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Utility;
using Revit.IFC.Common.Enums;
using Revit.IFC.Import.Enums;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCIndexedPolygonalFace : IFCRepresentationItem
   {
      IList<int> m_CoordIndex = null;
      IList<IList<int>> m_InnerCoordIndices = null;

      protected IFCIndexedPolygonalFace()
      {
      }

      public IList<int> CoordIndex
      {
         get { return m_CoordIndex; }
         protected set { m_CoordIndex = value; }
      }

      public IList<IList<int>> InnerCoordIndices
      {
         get { return m_InnerCoordIndices; }
         protected set { m_InnerCoordIndices = value; }
      }

      protected IFCIndexedPolygonalFace(IFCAnyHandle item)
      {
         Process(item);
      }

      protected override void Process(IFCAnyHandle ifcIndexPolygonalFace)
      {
         base.Process(ifcIndexPolygonalFace);

         IList<int> coordIndex = IFCAnyHandleUtil.GetAggregateIntAttribute<List<int>>(ifcIndexPolygonalFace, "CoordIndex");
         if (coordIndex != null)
            if (coordIndex.Count >= 3)
               CoordIndex = coordIndex;

         if (IFCAnyHandleUtil.IsTypeOf(ifcIndexPolygonalFace, IFCEntityType.IfcIndexedPolygonalFaceWithVoids))
         {
            IList<IList<int>> innerCoordIndices = IFCImportHandleUtil.GetListOfListOfIntegerAttribute(ifcIndexPolygonalFace, "InnerCoordIndices");
            if (innerCoordIndices != null)
               if (innerCoordIndices.Count > 0)
                  InnerCoordIndices = innerCoordIndices;
         }
      }

      public static IFCIndexedPolygonalFace ProcessIFCIndexedPolygonalFace(IFCAnyHandle ifcIndexedPolygonalFace)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcIndexedPolygonalFace))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcIndexedPolygonalFace);
            return null;
         }

         IFCEntity indexedPolygonalFace;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcIndexedPolygonalFace.StepId, out indexedPolygonalFace))
            indexedPolygonalFace = new IFCIndexedPolygonalFace(ifcIndexedPolygonalFace);
         return (indexedPolygonalFace as IFCIndexedPolygonalFace);
      }
   }
}