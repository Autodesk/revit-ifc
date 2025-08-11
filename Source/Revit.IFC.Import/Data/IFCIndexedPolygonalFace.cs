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
         if (IsValidCoordList(coordIndex))
            CoordIndex = coordIndex;

         if (IFCAnyHandleUtil.IsTypeOf(ifcIndexPolygonalFace, IFCEntityType.IfcIndexedPolygonalFaceWithVoids))
         {
            IList<IList<int>> innerCoordIndices = IFCImportHandleUtil.GetListOfListOfIntegerAttribute(ifcIndexPolygonalFace, "InnerCoordIndices");
            if ((innerCoordIndices?.Count ?? 0) >= 1)
            {
               IList<IList<int>> validInnerCoordindices = CreateValidInnerCoordList(innerCoordIndices);
               if ((validInnerCoordindices?.Count ?? 0) >= 3)
               {
                  InnerCoordIndices = validInnerCoordindices;
               }
            }
         }
      }

      /// <summary>
      /// Validate a list of coordinates, which will comprise a Face.  There should be at least three.
      /// The coordinates are indices into an IfcPolygonalFaceSet STEP.
      /// </summary>
      /// <param name="coordList">List of coordinate indices.</param>
      /// <returns>True if valid, False otherwise.</returns>
      public static bool IsValidCoordList(IList<int> coordList) => ((coordList?.Count ?? 0) >= 3);

      /// <summary>
      /// Creates a list of list of coordinates.  There should be at least one list of coordinates, and each list should have no
      /// less than three entries.
      /// </summary>
      /// <param name="innerCoordList">List of list of coordinate indices.</param>
      /// <returns>A list of a list of valid coordinate indices.</returns>
      public static IList<IList<int>> CreateValidInnerCoordList(IList<IList<int>> innerCoordList)
      {
         IList<IList<int>> validInnerCoordList = new List<IList<int>>();
         foreach (IList<int> coordList in innerCoordList)
         {
            if (IsValidCoordList(coordList))
            {
               validInnerCoordList.Add(coordList);
            }
         }

         return validInnerCoordList;
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