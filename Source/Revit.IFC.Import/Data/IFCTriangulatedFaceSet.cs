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
   public class IFCTriangulatedFaceSet : IFCTessellatedFaceSet
   {
      IList<IList<double>> m_Normals = null;
      bool? m_Closed = null;
      IList<IList<int>> m_CoordIndex = null;
      IList<IList<int>> m_NormalIndex = null;         // Removed in IFC4-ADD2
      IList<int> m_PnIndex = null;                    // Added in IFC-ADD2

      protected IFCTriangulatedFaceSet()
      {
      }


      /// <summary>
      /// List of Normals from the Normals attribute
      /// </summary>
      public IList<IList<double>> Normals
      {
         get { return m_Normals; }
         protected set { m_Normals = value; }
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
      /// List of triangle indexes (index to vertices in the Coordinates attribute)
      /// </summary>
      public IList<IList<int>> CoordIndex
      {
         get { return m_CoordIndex; }
         protected set { m_CoordIndex = value; }
      }

      /// <summary>
      /// List of Normal indexes (index to the normals in the list of normals in the Normals attribute (no longer used in IFC4-ADD2. Index follows the Vertex index)
      /// </summary>
      public IList<IList<int>> NormalIndex
      {
         get { return m_NormalIndex; }
         protected set { m_NormalIndex = value; }
      }

      /// <summary>
      /// List of Point index to the coordinates list (new in IFC4-ADD2)
      /// </summary>
      public IList<int> PnIndex
      {
         get { return m_PnIndex; }
         protected set { m_PnIndex = value; }
      }

      protected IFCTriangulatedFaceSet(IFCAnyHandle item)
      {
         Process(item);
      }

      /// <summary>
      /// Process IfcTriangulatedFaceSet instance
      /// </summary>
      /// <param name="ifcTriangulatedFaceSet">the handle</param>
      protected override void Process(IFCAnyHandle ifcTriangulatedFaceSet)
      {
         base.Process(ifcTriangulatedFaceSet);

         IList<IList<double>> normals = IFCImportHandleUtil.GetListOfListOfDoubleAttribute(ifcTriangulatedFaceSet, "Normals");
         if (normals != null)
            if (normals.Count > 0)
               Normals = normals;

         bool? closed = IFCAnyHandleUtil.GetBooleanAttribute(ifcTriangulatedFaceSet, "Closed");
         if (closed != null)
            Closed = closed;

         IList<IList<int>> coordIndex = IFCImportHandleUtil.GetListOfListOfIntegerAttribute(ifcTriangulatedFaceSet, "CoordIndex");
         if (coordIndex != null)
            if (coordIndex.Count > 0)
               CoordIndex = coordIndex;

         IList<IList<int>> normalIndex;
         if (IFCImportFile.TheFile.SchemaVersion >= IFCSchemaVersion.IFC4Add2)
            normalIndex = coordIndex;
         else
            normalIndex = IFCImportHandleUtil.GetListOfListOfIntegerAttribute(ifcTriangulatedFaceSet, "NormalIndex");

         if (normalIndex != null)
            if (normalIndex.Count > 0)
               NormalIndex = normalIndex;

         if (IFCImportFile.TheFile.SchemaVersion >= IFCSchemaVersion.IFC4Add2)
         {
            IList<int> pnIndex = IFCAnyHandleUtil.GetAggregateIntAttribute<List<int>>(ifcTriangulatedFaceSet, "PnIndex");
            if (pnIndex != null)
               if (pnIndex.Count > 0)
                  PnIndex = pnIndex;
         }
      }

      protected override void CreateShapeInternal(IFCImportShapeEditScope shapeEditScope, Transform lcs, Transform scaledLcs, string guid)
      {
         using (BuilderScope bs = shapeEditScope.InitializeBuilder(IFCShapeBuilderType.TessellatedShapeBuilder))
         {
            base.CreateShapeInternal(shapeEditScope, lcs, scaledLcs, guid);

            TessellatedShapeBuilderScope tsBuilderScope = bs as TessellatedShapeBuilderScope;

            tsBuilderScope.StartCollectingFaceSet();

            // Create triangle face set from CoordIndex. We do not support the Normals yet at this point
            foreach (List<int> triIndex in CoordIndex)
            {
               // This is a defensive check in an unlikely situation that the index is larger than the data
               if (triIndex[0] > Coordinates.CoordList.Count || triIndex[1] > Coordinates.CoordList.Count || triIndex[2] > Coordinates.CoordList.Count)
               {
                  continue;
               }

               tsBuilderScope.StartCollectingFace(GetMaterialElementId(shapeEditScope));

               IList<XYZ> loopVertices = new List<XYZ>();

               for (int ii = 0; ii < 3; ++ii)
               {
                  int actualVIdx = triIndex[ii] - 1;
                  if (PnIndex != null)
                     actualVIdx = PnIndex[actualVIdx] - 1;
                  XYZ vv = Coordinates.CoordList[actualVIdx];
                  loopVertices.Add(vv);
               }

               IList<XYZ> transformedVertices = new List<XYZ>();
               foreach (XYZ vertex in loopVertices)
               {
                  transformedVertices.Add(scaledLcs.OfPoint(vertex));
               }

               // Check triangle that is too narrow (2 vertices are within the tolerance
               IList<XYZ> validVertices;
               IFCGeometryUtil.CheckAnyDistanceVerticesWithinTolerance(Id, shapeEditScope, transformedVertices, out validVertices);

               // We are going to catch any exceptions if the loop is invalid.  
               // We are going to hope that we can heal the parent object in the TessellatedShapeBuilder.
               bool bPotentiallyAbortFace = false;

               int count = validVertices.Count;
               if (validVertices.Count < 3)
               {
                  Importer.TheLog.LogComment(Id, "Too few distinct loop vertices (" + count + "), ignoring.", false);
                  bPotentiallyAbortFace = true;
               }
               else
               {
                  if (!tsBuilderScope.AddLoopVertices(Id, validVertices))
                     bPotentiallyAbortFace = true;
               }

               if (bPotentiallyAbortFace)
                  tsBuilderScope.AbortCurrentFace();
               else
                  tsBuilderScope.StopCollectingFace();
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
      /// Start processing the IfcTriangulatedFaceSet
      /// </summary>
      /// <param name="ifcTriangulatedFaceSet">the handle</param>
      /// <returns></returns>
      public static IFCTriangulatedFaceSet ProcessIFCTriangulatedFaceSet(IFCAnyHandle ifcTriangulatedFaceSet)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcTriangulatedFaceSet))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcTriangulatedFaceSet);
            return null;
         }

         IFCEntity triangulatedFaceSet;
         if (!IFCImportFile.TheFile.EntityMap.TryGetValue(ifcTriangulatedFaceSet.StepId, out triangulatedFaceSet))
            triangulatedFaceSet = new IFCTriangulatedFaceSet(ifcTriangulatedFaceSet);
         return (triangulatedFaceSet as IFCTriangulatedFaceSet);
      }
   }
}