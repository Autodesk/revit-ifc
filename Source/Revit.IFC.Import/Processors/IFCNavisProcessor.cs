using System;
using System.Collections.Generic;

using Autodesk.Revit.DB;
using Revit.IFC.Import.Data;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import
{

   public interface IElement
   {
      /// <summary>
      /// Provide the IElement object that describes the IFC type element for this object.
      /// </summary>
      void SetType(IElement eleType);

      /// <summary>
      /// Provide the geometry to be used to represent this object.
      /// </summary>
      void SetGeometry(IList<GeometryObject> geoms);

      /// <summary>
      /// Provide another element whose geometry should be used to also represent this object.
      /// This is instanced geometry.
      /// The transform maps the shared geometry into the coordinate space of this object.
      /// </summary>
      void SetGeometry(IElement geomInstEle, Transform transform);

      /// <summary>
      /// Set the local to project space, transform for this object.
      /// 
      /// For geometry to be correctly displayed it should be transformed first through 
      /// the instance geometry transform, if set in the above method, and then this transform.
      /// </summary>
      void SetTransform(Transform transform);

      /// <summary>
      /// These are the set of methods for providing the parameter value for this element.
      /// (I'm not sure if the ElementId one would ever be used, but its there in IFCPropertySet.AddParameterXXX)
      /// </summary>

      void AddParameter(int parameterSetId, string parameterName, ElementId parameterValue);
      void AddParameter(int parameterSetId, string parameterName, bool parameterValue);
      void AddParameter(int parameterSetId, string parameterName, int parameterValue);
      void AddParameter(int parameterSetId, string parameterName, string parameterValue);
      void AddParameter(int parameterSetId, string parameterName, double parameterValue, ForgeTypeId specTypeId, ForgeTypeId unitsTypeId);
   }
   
   public class IFCNavisProcessor : IIFCFileProcessor
   {
      public virtual IElement GetElementFromStepId(int ifcId)
      {
         // NAVIS_TODO: This is inside Navis code.
         return null;
      }

      public virtual IElement CreateElement(int ifcId, string ifcGuid, string ifcEntityType, int categoryId)
      {
         // NAVIS_TODO: This is inside Navis code.
         return null;
      }

      public virtual void CreateTypeElement(int ifcId, string ifcGuid, string ifcEntityType, int categoryId)
      {
         // NAVIS_TODO: This is inside Navis code.
         return;
      }

      /// <summary>
      /// Report the units that length values with be in for geometry and transforms.
      /// </summary>
      /// <param name="units">The length unit.</param>
      public virtual void ReportLengthUnits(ForgeTypeId units)
      {
         // NAVIS_TODO: This is inside Navis code.
         return;
      }

      public bool CreateOrUpdateElement(int ifcId, string ifcGuid, string ifcEntityType, int categoryId,
         IList<GeometryObject> geomObjs)
      {
         if (ifcId <= 0)
            return true;

         // NAVIS_TODO: This is a bit of a hack to minimize Navis code in the "main" Revit code.
         if (geomObjs == null)
         {
            // This is a work around for the fact that some entity types have a valid CreatedElementId,
            // (otherwise we wouldn't even get to here) but we haven't yet created an IElement.
            //
            // The real solution would probably be to find all of those cases and fix them.
            // ... and that is what would need to happen if the test in CreateParameters was updated
            // to test if an IElement existed instead of testing whether CreatedElementId was set.

            IElement altElement = GetElementFromStepId(ifcId);
            if (altElement != null)
               return true;
         }

         IElement ele = CreateElement(ifcId, ifcGuid, ifcEntityType, categoryId);
         if (ele != null)
            ele.SetGeometry(geomObjs);

         return true;
      }

      public void CreateElementType(int ifcId, string ifcGuid, string ifcEntityType, int categoryId)
      {
         // This is used in some places to create something not related to an IFCEntity
         if (ifcId > 0)
         {
            CreateTypeElement(ifcId, ifcGuid, ifcEntityType, categoryId);
         }
      }

      public void PostProcessMappedItem(IFCImportShapeEditScope shapeEditScope, IFCMappedItem mappedItem, Transform newLcs)
      {
         if (shapeEditScope.Creator != null)
         {
            // Copy some code from elsewhere to work out what the object was that got the geometry.
            int originalMappingSourceId = mappedItem.MappingSource.Id;
            int typeObjectId = originalMappingSourceId;
            IFCTypeProduct typeProduct = null;
            if (Importer.TheCache.RepMapToTypeProduct.TryGetValue(originalMappingSourceId, out typeProduct) && typeProduct != null)
            {
               typeObjectId = typeProduct.Id;
            }

            // Now get that type object
            var typeEle = GetElementFromStepId(typeObjectId);

            // The type element might not have been created, if its geometry processing failed.
            if (typeEle != null)
            {
               // We now need to set the type object geometry onto the entity.
               // The main reason for needing to do it here is because we have newLcs calculated, but we haven't yet got 
               // and IElement for the entity, so first create one.
               var instEle = CreateElement(shapeEditScope.Creator.Id, shapeEditScope.Creator.GlobalId, shapeEditScope.Creator.EntityType.ToString(), shapeEditScope.Creator.CategoryId.IntegerValue);
               instEle.SetGeometry(typeEle, newLcs);

               // Hack
               // Add some bogus geometry so that later on we think that this object matters.
               // Without this we don't create a DirectShape later on and so CreatedElementId is not set.
               IList<GeometryObject> instances = DirectShape.CreateGeometryInstance(shapeEditScope.Document, originalMappingSourceId.ToString(), newLcs);
               foreach (GeometryObject instance in instances)
                  shapeEditScope.AddGeometry(IFCSolidInfo.Create(mappedItem.Id, instance));
            }
         }
      }

      public void PostProcessProduct(int ifcId, IFCTypeObject typeObject, IFCImportShapeEditScope shapeEditScope,
         DirectShape shape, Transform lcs, IList<GeometryObject> directShapeGeometries)
      {
         var ele = GetElementFromStepId(ifcId);

         if (!Importer.TheProcessor.ApplyTransforms)
         {
            // Now provide the transform for the consumer to use with the untransformed geometry.
            // If a transform was already set by calling IElement.SetGeometry(IElement geomInstEle, Transform transform)
            // then lcs should be PreMultiplied into that existing transform in the IElement implementation.
            ele.SetTransform(lcs);
         }

         // If we are instanced, then really we shouldn't have any directShapeGeometries, but currently
         // if we don't then CreateParameters doesn't run, so we have some directShapeGeometries to fix it.
         // So the implementation of SetGeometry here doesn't overwrite the existing set instanced geometry
         // with this.
         ele.SetGeometry(directShapeGeometries);

         if (typeObject != null)
         {
            var typeEle = GetElementFromStepId(typeObject.Id);

            ele.SetType(typeEle);
         }
      }

      public void PostProcessProject()
      {
         var application = IFCImportFile.TheFile.Document.Application;
         double vertexTolerance = application.VertexTolerance;
         double shortCurveTolerance = application.ShortCurveTolerance;

         if (ScaleValues)
         {
            ReportLengthUnits(UnitTypeId.Feet);
            VertexTolerance = vertexTolerance;
            ShortCurveTolerance = shortCurveTolerance;
         }
         else
         {
            var units = IFCImportFile.TheFile.IFCUnits.GetIFCProjectUnit(SpecTypeId.Length);

            if (units != null)
            {
               ReportLengthUnits(units.Unit);
               VertexTolerance = vertexTolerance / units.ScaleFactor;
               ShortCurveTolerance = shortCurveTolerance / units.ScaleFactor;
               IFCImportFile.TheFile.OneHundrethOfAFoot = 0.01 / units.ScaleFactor;
               IFCImportFile.TheFile.OneMillimeter = UnitUtils.Convert(1.0, UnitTypeId.Millimeters, units.Unit);
            }
         }
      }

      public bool? ProcessParameter(int objDefId, ForgeTypeId specTypeId, ForgeTypeId unitsTypeId,
         int parameterSetId, string parameterName, double parameterValue)
      {
         if (unitsTypeId == null)
         {
            if (!ScaleValues)
            {
               // We can look up the units when the values are not scaled.
               var units = IFCImportFile.TheFile.IFCUnits.GetIFCProjectUnit(specTypeId);
               if (units != null)
               {
                  unitsTypeId = units.Unit;
               }
               else
               {
                  // No units - is this the current unit for that ?
                  unitsTypeId = UnitTypeId.General;
               }
            }
            else
            {
               // Get the internal units for the specTypeId
            }
         }

         GetElementFromStepId(objDefId).AddParameter(parameterSetId, parameterName, parameterValue, specTypeId, unitsTypeId);
         return true;
      }

      public bool? ProcessParameter(int objDefId, int parameterSetId, string parameterName, string parameterValue)
      {
         GetElementFromStepId(objDefId).AddParameter(parameterSetId, parameterName, parameterValue);
         return true;
      }

      public bool? ProcessParameter(int objDefId, int parameterSetId, string parameterName, int parameterValue)
      {
         GetElementFromStepId(objDefId).AddParameter(parameterSetId, parameterName, parameterValue);
         return true;
      }

      public bool? ProcessParameter(int objDefId, int parameterSetId, string parameterName, ElementId parameterValue)
      {
         GetElementFromStepId(objDefId).AddParameter(parameterSetId, parameterName, parameterValue);
         return true;
      }

      public bool? ProcessParameter(int objDefId, int parameterSetId, string parameterName, bool parameterValue)
      {
         GetElementFromStepId(objDefId).AddParameter(parameterSetId, parameterName, parameterValue);
         return true;
      }

      public bool SetParameter(Element element, BuiltInParameter parameterId, string value)
      {
         return false;
      }

      public void PostProcessRepresentationMap(int typeId, IFCImportShapeEditScope shapeEditScope,
         IList<Curve> mappedCurves, IList<GeometryObject> mappedSolids, DirectShapeType directShapeType)
      {
         GetElementFromStepId(typeId).SetGeometry(mappedSolids);
      }

      public void PostProcessSite(IFCSite site)
      {
         var ele = GetElementFromStepId(site.Id);
         if (site.RefLatitude.HasValue)
         {
            ele.AddParameter(-1, "Latitude", site.RefLatitude.Value, SpecTypeId.Angle, UnitTypeId.Degrees);
         }
         if (site.RefLongitude.HasValue)
         {
            ele.AddParameter(-1, "Longitude", site.RefLongitude.Value, SpecTypeId.Angle, UnitTypeId.Degrees);
         }

         var lengthUnits = UnitTypeId.Feet;

         if (!ScaleValues)
         {
            var lU = IFCImportFile.TheFile.IFCUnits.GetIFCProjectUnit(SpecTypeId.Length);
            if (lU != null)
            {
               lengthUnits = lU.Unit;
            }
         }

         ele.AddParameter(-1, "Elevation", site.RefElevation, SpecTypeId.Length, lengthUnits);
         ele.AddParameter(-1, "LandTitleNumber", site.LandTitleNumber);

         if (site.ObjectLocation != null)
         {
            // If we are reporting Latitude and Longitude, then we need a transform, to know 
            // where this survey point is, in our world space.
            ele.SetTransform(site.ObjectLocation.TotalTransform);
         }
      }

      public bool ShouldFixFarAwayLocation { get => false; }

      // For this initial implementation of a Navis processor, 
      // we have no scaling and no applied transforms.
      
      public bool ScaleValues { get => false; }

      public bool ApplyTransforms { get => false; }

      public double VertexTolerance { get; set; } = 0.0;

      public double ShortCurveTolerance { get; set; } = 0.0;
   }
}
