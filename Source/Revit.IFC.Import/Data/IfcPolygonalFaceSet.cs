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
   public class IFCPolygonalFaceSet : IFCTessellatedFaceSet
   {
      bool? m_Closed = null;
      IList<int> m_PnIndex = null;
      IList<IFCIndexedPolygonalFace> m_Faces = null;

      protected IFCPolygonalFaceSet()
      {
      }

      /// <summary>
      /// Closed attribute
      /// </summary>
      public bool? Closed
      {
         get { return m_Closed; }
         protected set { m_Closed = value; }
      }

      /// <summary>
      /// PnIndex attribute
      /// </summary>
      public IList<int> PnIndex
      {
         get { return m_PnIndex; }
         protected set { m_PnIndex = value; }
      }

      public IList<IFCIndexedPolygonalFace> Faces
      {
         get { return m_Faces; }
         protected set { m_Faces = value; }
      }

      protected IFCPolygonalFaceSet(IFCAnyHandle item)
      {
         Process(item);
      }

      protected override void Process(IFCAnyHandle ifcPolygonalFaceSet)
      {
         base.Process(ifcPolygonalFaceSet);

         bool? closed = IFCAnyHandleUtil.GetBooleanAttribute(ifcPolygonalFaceSet, "Closed");
         if (closed != null)
            Closed = closed;

         IList<IFCAnyHandle> facesHnds = IFCAnyHandleUtil.GetAggregateInstanceAttribute<List<IFCAnyHandle>>(ifcPolygonalFaceSet, "Faces");
         if (facesHnds != null)
         {
            if (facesHnds.Count > 0)
               Faces = new List<IFCIndexedPolygonalFace>();
            foreach (IFCAnyHandle facesHnd in facesHnds)
            {
               Faces.Add(IFCIndexedPolygonalFace.ProcessIFCIndexedPolygonalFace(facesHnd));
            }
         }
      }

      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, 
         Transform scaledLcs, string guid)
      {
         using (BuilderScope bs = shapeEditScope.InitializeBuilder(IFCShapeBuilderType.TessellatedShapeBuilder))
         {
            base.CreateShapeInternal(shapeEditScope, scaledLcs, guid);

            TessellatedShapeBuilderScope tsBuilderScope = bs as TessellatedShapeBuilderScope;

            tsBuilderScope.StartCollectingFaceSet();

            // Create the face set from IFCIndexedPolygonalFace
            foreach (IFCIndexedPolygonalFace face in Faces)
            {
               // TODO: Consider adding ability to triangulate here.
               tsBuilderScope.StartCollectingFace(GetMaterialElementId(shapeEditScope), false);

               IList<XYZ> loopVertices = new List<XYZ>();
               foreach (int vertInd in face.CoordIndex)
               {
                  int actualVIdx = vertInd - 1;       // IFC starts the list position at 1
                  if (PnIndex != null)
                     actualVIdx = PnIndex[actualVIdx] - 1;
                  XYZ vertex = Coordinates.CoordList[actualVIdx];
                  loopVertices.Add(scaledLcs.OfPoint(vertex));
               }
               List<XYZ> validVertices;
               IFCGeometryUtil.CheckAnyDistanceVerticesWithinTolerance(Id, shapeEditScope, loopVertices, out validVertices);

               bool bPotentiallyAbortFace = false;
               if (!tsBuilderScope.AddLoopVertices(Id, validVertices))
                  bPotentiallyAbortFace = true;

               // Handle holes
               if (face.InnerCoordIndices != null)
               {
                  foreach (IList<int> innerLoop in face.InnerCoordIndices)
                  {
                     IList<XYZ> innerLoopVertices = new List<XYZ>();
                     foreach (int innerVerIdx in innerLoop)
                     {
                        int actualVIdx = innerVerIdx - 1;
                        if (PnIndex != null)
                           actualVIdx = PnIndex[actualVIdx] - 1;
                        XYZ vertex = Coordinates.CoordList[actualVIdx];
                        // add vertex to the loop
                        innerLoopVertices.Add(scaledLcs.OfPoint(vertex));
                     }
                     List<XYZ> validInnerV;
                     IFCGeometryUtil.CheckAnyDistanceVerticesWithinTolerance(Id, shapeEditScope, innerLoopVertices, out validInnerV);

                     if (!tsBuilderScope.AddLoopVertices(Id, validInnerV))
                        bPotentiallyAbortFace = true;
                  }
               }

               tsBuilderScope.StopCollectingFace(!bPotentiallyAbortFace, false);
            }

            IList<GeometryObject> createdGeometries = tsBuilderScope.CreateGeometry(guid);
            if (createdGeometries != null)
            {
               foreach (GeometryObject createdGeometry in createdGeometries)
               {
                  shapeEditScope.AddGeometry(IFCSolidInfo.Create(Id, createdGeometry));
               }
            }
         }
      }

      /// <summary>
      /// Start processing the IfcPolygonalSet
      /// </summary>
      /// <param name="ifcPolygonalFaceSet">the handle</param>
      /// <returns></returns>
      public static IFCPolygonalFaceSet ProcessIFCPolygonalFaceSet(IFCAnyHandle ifcPolygonalFaceSet)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcPolygonalFaceSet))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcPolygonalFaceSet);
            return null;
         }

         IFCEntity polygonalFaceSet;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcPolygonalFaceSet.StepId, out polygonalFaceSet))
            polygonalFaceSet = new IFCPolygonalFaceSet(ifcPolygonalFaceSet);
         return (polygonalFaceSet as IFCPolygonalFaceSet);
      }
   }
}