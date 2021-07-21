using System;
using System.Collections.Generic;

using Autodesk.Revit.DB;
using Revit.IFC.Import.Data;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import
{
   public class IFCDefaultProcessor : IIFCFileProcessor
   {
      public bool CreateOrUpdateElement(int ifcId, string ifcGuid, string ifcEntityType, int categoryId,
         IList<GeometryObject> geomObjs)
      {
         return false;
      }

      public void CreateElementType(int ifcId, string ifcGuid, string ifcEntityType, int categoryId)
      {
      }

      public void PostProcessMappedItem(IFCImportShapeEditScope shapeEditScope, IFCMappedItem mappedItem, Transform newLcs)
      {
         // We should now have a type object that contains geometry.
         IFCRepresentationMap mappingSource = mappedItem.MappingSource;

         IList<GeometryObject> instances = DirectShape.CreateGeometryInstance(shapeEditScope.Document, mappingSource.Id.ToString(), newLcs);
         foreach (GeometryObject instance in instances)
            shapeEditScope.AddGeometry(IFCSolidInfo.Create(mappedItem.Id, instance));
      }

      public void PostProcessProduct(int ifcId, IFCTypeObject typeObject, IFCImportShapeEditScope shapeEditScope,
         DirectShape shape, Transform lcs, IList<GeometryObject> directShapeGeometries)
      {
         if (shape == null)
            return;

         shape.SetShape(directShapeGeometries);
         shapeEditScope.SetPlanViewRep(shape);

         if (typeObject != null && typeObject.CreatedElementId != ElementId.InvalidElementId)
            shape.SetTypeId(typeObject.CreatedElementId);
      }

      public bool? ProcessParameter(int objDefId, ForgeTypeId specTypeId, ForgeTypeId unitsTypeId,
         int parameterSetId, string parameterName, double parameterValue)
      {
         return null;
      }

      public bool? ProcessParameter(int objDefId, int parameterSetId, string parameterName, string parameterValue)
      {
         return null;
      }

      public bool? ProcessParameter(int objDefId, int parameterSetId, string parameterName, int parameterValue)
      {
         return null;
      }

      public bool? ProcessParameter(int objDefId, int parameterSetId, string parameterName, ElementId parameterValue)
      {
         return null;
      }

      public bool? ProcessParameter(int objDefId, int parameterSetId, string parameterName, bool parameterValue)
      {
         return null;
      }

      public bool SetParameter(Element element, BuiltInParameter parameterId, string value)
      {
         Parameter parameter = element?.get_Parameter(parameterId);
         if (parameter == null || parameter.StorageType != StorageType.String || parameter.IsReadOnly)
            return false;

         parameter.Set(value);
         return true;
      }

      public void PostProcessRepresentationMap(int typeId, IFCImportShapeEditScope shapeEditScope,
         IList<Curve> mappedCurves, IList<GeometryObject> mappedSolids, DirectShapeType directShapeType)
      {
         directShapeType.AppendShape(mappedSolids);
         if (mappedCurves.Count != 0)
            shapeEditScope.SetPlanViewRep(directShapeType);
      }

      public void PostProcessSite(IFCSite site)
      {
      }

      public void PostProcessProject()
      {
         var application = IFCImportFile.TheFile.Document.Application;
         VertexTolerance = application.VertexTolerance;
         ShortCurveTolerance = application.ShortCurveTolerance;
      }

      public bool ShouldFixFarAwayLocation { get => true; }

      public bool ScaleValues { get => true; }

      public bool ApplyTransforms { get => true; }

      public double VertexTolerance { get; set; } = 0.0;

      public double ShortCurveTolerance { get; set; } = 0.0;
   }
}
