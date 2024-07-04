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
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Revit.IFC.Common.Enums;
using Revit.IFC.Common.Utility;
using Revit.IFC.Import.Geometry;
using Revit.IFC.Import.Utility;

namespace Revit.IFC.Import.Data
{
   public class IFCVoidInfo : IFCSolidInfo
   {
      public Transform TotalTransform
      {
         get; set;
      }

      public IFCVoidInfo(IFCSolidInfo solid)
         : base(solid.Id, solid.GeometryObject)
      {
         this.RepresentationIdentifier = solid.RepresentationIdentifier;
      }
   }


   /// <summary>
   /// Represents an IfcProduct.
   /// </summary>
   public abstract class IFCProduct : IFCObject
   {
      /// <summary>
      /// The id of the corresponding IfcTypeProduct, if any.
      /// </summary>
      public int TypeId { get; protected set; } = 0;

      /// <summary>
      /// The list of solids created for the associated element.
      /// </summary>
      public IList<IFCSolidInfo> Solids { get; } = new List<IFCSolidInfo>();

      /// <summary>
      /// The list of voids created for the associated element.
      /// </summary>
      public IList<IFCVoidInfo> Voids { get; } = new List<IFCVoidInfo>();

      /// <summary>
      /// The list of curves created for the associated element, for use in plan views.
      /// </summary>
      public IList<Curve> FootprintCurves { get; } = new List<Curve>();

      /// <summary>
      /// The names of the presentation layers associated with the representations and representation items.
      /// </summary>
      public ISet<string> PresentationLayerNames { get; } = new SortedSet<string>();

      /// <summary>
      /// The one product representation of the product.
      /// </summary>
      public IFCProductRepresentation ProductRepresentation { get; protected set; } = null;

      /// <summary>
      /// The IfcSpatialStructureElement that contains the IfcElement.
      /// </summary>
      public IFCSpatialStructureElement ContainingStructure { get; set; } = null;

      /// <summary>
      /// The local coordinate system of the IfcProduct.
      /// </summary>
      public IFCLocation ObjectLocation { get; protected set; } = null;

      /// <summary>
      /// Default constructor.
      /// </summary>
      protected IFCProduct()
      {

      }

      /// <summary>
      /// Processes IfcProduct attributes.
      /// </summary>
      /// <param name="ifcProduct">The IfcProduct handle.</param>
      protected override void Process(IFCAnyHandle ifcProduct)
      {
         // We are going to process the IfcObjectPlacement before we do the base Process call.  The reason for this is that we'd like to
         // process the IfcSite object placement before any of its children, so that the RelativeToSite can be properly set.
         // If this becomes an issue, we can instead move this to after the base.Process, and calculate RelativeToSite as a post-process step.
         ProcessObjectPlacement(ifcProduct);

         base.Process(ifcProduct);

         // Don't even process IfcProductRepresentation if this IfcProduct was imported via Hybrid import.
         if (IFCImportHybridInfo.IsValidElementId(IFCImportHybridInfo.GetHybridMapInformation(Id)))
         {
            return;
         }

         IFCAnyHandle ifcProductRepresentation = IFCImportHandleUtil.GetOptionalInstanceAttribute(ifcProduct, "Representation");
         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(ifcProductRepresentation))
         {
            ProductRepresentation = IFCProductRepresentation.ProcessIFCProductRepresentation(ifcProductRepresentation);
         }
      }

      static public BoundingBoxXYZ ProjectScope { get; set; } = null;

      private void AddLocationToPlacementBoundingBox()
      {
         if (!Importer.TheProcessor.TryToFixFarawayOrigin)
            return;

         if (!(this is IFCSpatialStructureElement))
         {
            XYZ lcsOrigin = ObjectLocation?.TotalTransform?.Origin ?? XYZ.Zero;
            if (ProjectScope == null)
            {
               ProjectScope = new BoundingBoxXYZ();
               ProjectScope.Min = lcsOrigin;
               ProjectScope.Max = lcsOrigin;
            }
            else
            {
               ProjectScope.Min = new XYZ(
                  Math.Min(ProjectScope.Min.X, lcsOrigin.X),
                  Math.Min(ProjectScope.Min.Y, lcsOrigin.Y),
                  Math.Min(ProjectScope.Min.Z, lcsOrigin.Z)
                  );
               ProjectScope.Max = new XYZ(
                  Math.Max(ProjectScope.Min.X, lcsOrigin.X),
                  Math.Max(ProjectScope.Min.Y, lcsOrigin.Y),
                  Math.Max(ProjectScope.Min.Z, lcsOrigin.Z)
                  );
            }
         }
      }

      /// <summary>
      /// Creates or populates Revit element params based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      /// <param name="element">The element.</param>
      protected override void CreateParametersInternal(Document doc, Element element)
      {
         base.CreateParametersInternal(doc, element);

         if (element != null)
         {
            Category category = IFCPropertySet.GetCategoryForParameterIfValid(element, Id);

            // Set "IfcPresentationLayer" parameter.
            string ifcPresentationLayer = null;
            foreach (string currLayer in PresentationLayerNames)
            {
               if (string.IsNullOrWhiteSpace(currLayer))
                  continue;

               if (ifcPresentationLayer == null)
                  ifcPresentationLayer = currLayer;
               else
                  ifcPresentationLayer += "; " + currLayer;
            }

            if (ifcPresentationLayer != null)
               ParametersToSet.AddStringParameter(doc, element, category, this, "IfcPresentationLayer", ifcPresentationLayer, Id);

            // Set the container name of the element.
            string containerName = ContainingStructure?.Name;
            if (containerName != null)
            {
               ParametersToSet.AddStringParameter(doc, element, category, this, "IfcSpatialContainer", containerName, Id);
               ParametersToSet.AddStringParameter(doc, element, category, this, "IfcSpatialContainer GUID", ContainingStructure.GlobalId, Id);
            }
         }
      }

      /// <summary>
      /// Private function to determine whether an IFCProduct directly contains valid geometry.
      /// </summary>
      /// <remarks>
      /// For Hybrid IFC Import, ProductRepresentation may not exist, but Geometry is instead within a GeometryInstance.
      /// </remarks>
      /// <returns>True if the IFCProduct directly contains valid geometry.</returns>
      private bool HasValidTopLevelGeometry()
      {
         // If this IfcProduct was imported via Hybrid import, ProductRepresentation may not exist.  Check for DirectShape instead.
         ElementId directShapeId = IFCImportHybridInfo.GetHybridMapInformation(Id);
         if (IFCImportHybridInfo.IsValidElementId(directShapeId))
         {
            // Or continue the check and require that the directShapeId point to
            // a DirectShape (not DirectShapeType) -- unsure!
            return Importer.TheHybridInfo.IsValidDirectShape(directShapeId);
         }

         return (ProductRepresentation != null && ProductRepresentation.IsValid());
      }

      /// <summary>
      /// Private function to determine whether an IFCProduct contains geometry in a sub-element.
      /// </summary>
      /// <param name="visitedEntities">A list of already visited entities, to avoid infinite recursion.</param>
      /// <returns>True if the IFCProduct directly or indirectly contains geometry.</returns>
      private bool HasValidSubElementGeometry(IList<IFCEntity> visitedEntities)
      {
         foreach (IFCObjectDefinition objDef in ComposedObjectDefinitions)
         {
            if (visitedEntities.Contains(objDef))
               continue;

            visitedEntities.Add(objDef);

            if (!(objDef is IFCProduct))
               continue;

            if ((objDef as IFCProduct).HasValidTopLevelGeometry())
               return true;

            if ((objDef as IFCProduct).HasValidSubElementGeometry(visitedEntities))
               return true;
         }

         return false;
      }

      /// <summary>
      /// Cut a IFCSolidInfo by the voids in this IFCProduct, if any.
      /// </summary>
      /// <param name="solidInfo">The solid information.</param>
      /// <param name="createdVoids">Extra voids from AnyCAD-created openings.</param>
      /// <returns>False if the return solid is empty; true otherwise.</returns>
      protected override bool CutSolidByVoids(IFCSolidInfo solidInfo, IList<Solid> createdVoids)
      {
         // We only cut "Body" representation items.
         if (solidInfo.RepresentationIdentifier != IFCRepresentationIdentifier.Body)
            return true;

         IList<IFCVoidInfo> voidsToUse = null;
         List<IFCVoidInfo> partVoids = null;

         IList <IFCVoidInfo> parentVoids = (Decomposes as IFCProduct)?.Voids;
         if (parentVoids != null)
         {
            partVoids = new List<IFCVoidInfo>();
            partVoids.AddRange(Voids);
            partVoids.AddRange(parentVoids);
            voidsToUse = partVoids;
         }
         else
         {
            voidsToUse = Voids;
         }

         int numVoids = voidsToUse.Count;
         if (numVoids == 0)
            return true;

         if (!(solidInfo.GeometryObject is Solid))
         {
            string typeName = (solidInfo.GeometryObject is Mesh) ? "mesh" : "instance";
            Importer.TheLog.LogError(Id, "Can't cut " + typeName + " geometry, ignoring " + numVoids + " void(s).", false);
            return true;
         }

         List<Tuple<Solid, Transform, int>> allVoids = new List<Tuple<Solid, Transform, int>>();

         if (createdVoids != null)
         {
            foreach (Solid createdVoid in createdVoids)
            {
               if (createdVoid != null)
               {
                  allVoids.Add(Tuple.Create<Solid, Transform, int>(createdVoid, null, -1));
               }
            }
         }

         foreach (IFCVoidInfo voidInfo in voidsToUse)
         {
            Solid voidObject = voidInfo.GeometryObject as Solid;
            int voidId = voidInfo.Id;
            if (voidObject == null)
            {
               Importer.TheLog.LogError(Id, "Can't cut Solid geometry with a Mesh (# " + voidId + "), ignoring.", false);
               continue;
            }

            Transform voidTransform = voidInfo.TotalTransform;
            allVoids.Add(Tuple.Create(voidObject, voidTransform, voidId));
         }

         foreach (Tuple<Solid, Transform, int> currentVoid in allVoids)
         {
            Solid voidObject = currentVoid.Item1;
            Transform voidTransform = currentVoid.Item2;
            int voidId = currentVoid.Item3;

            if (voidTransform != null)
            {
               // Transform the void into the space of the solid.
               Transform voidToSolidTrf = ObjectLocation.TotalTransform.Inverse.Multiply(voidTransform);
               if (voidToSolidTrf.IsIdentity == false)
               {
                  voidObject = SolidUtils.CreateTransformed(voidObject, voidToSolidTrf);
               }
            }

            solidInfo.GeometryObject = IFCGeometryUtil.ExecuteSafeBooleanOperation(solidInfo.Id, voidId,
               (solidInfo.GeometryObject as Solid), voidObject, BooleanOperationsType.Difference, null);

            if (solidInfo.GeometryObject == null || (solidInfo.GeometryObject as Solid).Faces.IsEmpty)
               return false;
         }

         return true;
      }

      private Transform CalculateLocalCoordinateSystem()
      {
         Transform lcs = IFCImportFile.TheFile.IFCProject.WorldCoordinateSystem;
         if (lcs == null)
            return ObjectLocation?.TotalTransform ?? Transform.Identity;
         
         if (ObjectLocation != null)
            return lcs.Multiply(ObjectLocation.TotalTransform);

         return lcs;
      }

      private IList<Solid> GetCreatedGeometries(Document doc, IFCProduct opening, Options geometryOptions)
      {
         ElementId createdOpeningId = IFCImportHybridInfo.GetHybridMapInformation(opening.Id);
         if (IFCImportHybridInfo.IsValidElementId(createdOpeningId))
         {
            DirectShape openingElement = doc.GetElement(createdOpeningId) as DirectShape;
            if (openingElement != null)
            {
               GeometryElement geometryElement = openingElement.get_Geometry(geometryOptions);

               SolidMeshGeometryInfo solidInfo = new SolidMeshGeometryInfo();
               solidInfo.CollectSolidMeshGeometry(geometryElement, Importer.TheCache.AllocatedGeometryObjectCache);
               return solidInfo.GetSolids();
            }
            else
            {
               Importer.TheLog.LogError(Id, "Object created in legacy mode missing opening information.", false);
            }
         }

         return null;
      }

      /// <summary>
      /// Creates or populates Revit elements based on the information contained in this class.
      /// </summary>
      /// <param name="doc">The document.</param>
      protected override void Create(Document doc)
      {
         bool preventInstances = false;
         IFCElement element = this as IFCElement;
         List<Solid> createdVoids = new List<Solid>();
         
         if (element != null)
         {
            preventInstances = this is IFCOpeningElement;

            Options geometryOptions = new Options();

            foreach (IFCFeatureElement opening in element.Openings)
            {
               try
               {
                  IList<Solid> createdGeometries = GetCreatedGeometries(doc, opening, geometryOptions);
                  if (createdGeometries != null)
                  {
                     createdVoids.AddRange(createdGeometries);
                  }

                  preventInstances = true;
                  // Create the actual Revit element based on the IFCFeatureElement here.
                  ElementId openingId = CreateElement(doc, opening);

                  // This gets around the issue that the Boolean operation between the void(s) in the IFCFeatureElement and 
                  // the solid(s) in the IFCElement may use the Graphics Style of the voids in the resulting Solid(s), meaning 
                  // that some faces may disappear when we turn off the visibility of IfcOpeningElements.
                  IList<IFCSolidInfo> voids = IFCElement.CloneElementGeometry(doc, opening, this, true);
                  if (voids != null)
                  {
                     foreach (IFCSolidInfo voidGeom in voids)
                     {
                        IFCVoidInfo voidInfo = new IFCVoidInfo(voidGeom);
                        if (!Importer.TheProcessor.ApplyTransforms)
                        {
                           // If we aren't applying transforms, then the Voids and Solids will be
                           // in different coordinate spaces, so we need the transform of the 
                           // void, so we can transform it into the Solid coordinate space
                           voidInfo.TotalTransform = opening?.ObjectLocation?.TotalTransform;
                        }

                        Voids.Add(voidInfo);
                     }
                  }
               }
               catch (Exception ex)
               {
                  Importer.TheLog.LogError(opening.Id, ex.Message, false);
               }
            }
         }

         // If this entity will be a container DirectShape, it may not have any valid top-level geometry at this time.
         // Detect the situation and allow Element Creation to proceed for Hybrid Import only.
         if (HasValidTopLevelGeometry())
         {
            // IFCImportShapeEditScope will not create Body geometry for Hybrid IFC Import, but it may need to create other geometry.
            using (IFCImportShapeEditScope shapeEditScope = IFCImportShapeEditScope.Create(doc, this))
            {
               Transform lcs = CalculateLocalCoordinateSystem();

               shapeEditScope.PreventInstances = preventInstances;

               // If we are not applying transforms to the geometry, then pass in the identity matrix.
               // Lower down this method we then pass lcs to the consumer element, so that it can apply
               // the transform as required.
               Transform transformToUse = Importer.TheProcessor.ApplyTransforms ? lcs : Transform.Identity;
               bool applyHybridOffset = Importer.TheOptions.IsHybridImport && Importer.TheHybridInfo != null && ObjectLocation?.RelativeTo == null;
               if (applyHybridOffset)
               {
                  transformToUse.Origin += Importer.TheHybridInfo.LargeCoordinateOriginOffset;
               }

               // If Revit has already created a DirectShape from the IfcProduct, don't try and create a new representation.
               ElementId hybridDirectShapeElementId = IFCImportHybridInfo.GetHybridMapInformation(Id);
               if (IFCImportHybridInfo.IsValidElementId(hybridDirectShapeElementId))
               {
                  CreatedGeometry = Importer.TheHybridInfo.HandleHybridProductCreation(this, hybridDirectShapeElementId);
                  CreatedElementId = hybridDirectShapeElementId;
               }
               else
               {
                  ProductRepresentation.CreateProductRepresentation(shapeEditScope, transformToUse, GlobalId);

                  int numSolids = Solids.Count;
                  // Attempt to cut each solid with each void.
                  for (int solidIdx = 0; solidIdx < numSolids; solidIdx++)
                  {
                     if (!CutSolidByVoids(Solids[solidIdx], createdVoids))
                     {
                        Solids.RemoveAt(solidIdx);
                        solidIdx--;
                        numSolids--;
                     }
                  }

                  bool addedCurves = shapeEditScope.AddPlanViewCurves(FootprintCurves, Id);

                  if ((numSolids > 0 || addedCurves))
                  {
                     if (GlobalId != null)
                     {
                        // If the GlobalId is null, this is a fake IfcProduct that we don't want to create into a DirectShape, or
                        // add to the caches in any way.  We only wanted to gather its geometry.
                        DirectShape shape = Importer.TheCache.UseElementByGUID<DirectShape>(doc, GlobalId);

                        if (shape == null)
                        {
                           shape = IFCElementUtil.CreateElement(doc, GetCategoryId(doc), GlobalId, null, Id, EntityType);
                        }

                        List<GeometryObject> directShapeGeometries = new List<GeometryObject>();
                        foreach (IFCSolidInfo geometryObject in Solids)
                        {
                           // We need to check if the solid created is good enough for DirectShape.  If not, warn and use a fallback Mesh.
                           GeometryObject currObject = geometryObject.GeometryObject;
                           
                           if (currObject != null)
                           {
                              IList<GeometryObject> adjustedObjects = IFCGeometryUtil.AdjustGeometryObjectsIfNeeded(currObject, shape, Id);
                              if (adjustedObjects != null)
                              {
                                 directShapeGeometries.AddRange(adjustedObjects);
                              }
                              else
                              {
                                 directShapeGeometries.Add(currObject);
                              }
                           }
                        }

                        // We will use the first IfcTypeObject id, if it exists.  In general, there should be 0 or 1.
                        IFCTypeObject typeObjectToUse = null;
                        foreach (IFCTypeObject typeObject in TypeObjects)
                        {
                           if (typeObject.IsValidForCreation && typeObject.CreatedElementId != ElementId.InvalidElementId)
                           {
                              typeObjectToUse = typeObject;
                              break;
                           }
                        }

                        if (!Importer.TheProcessor.PostProcessProduct(Id, typeObjectToUse?.Id, lcs,
                           directShapeGeometries))
                        {
                           if (shape != null)
                           {
                              shape.SetShape(directShapeGeometries);
                              shapeEditScope.SetPlanViewRep(shape);

                              if (typeObjectToUse != null && typeObjectToUse.CreatedElementId != ElementId.InvalidElementId)
                                 shape.SetTypeId(typeObjectToUse.CreatedElementId);
                           }
                        }

                        PresentationLayerNames.UnionWith(shapeEditScope.PresentationLayerNames);

                        CreatedElementId = shape.Id;
                        CreatedGeometry = directShapeGeometries;
                     }
                  }
               }
            }
         }
         else
         {
            if (this is IFCElement || this is IFCGrid)
            {
               IList<IFCEntity> visitedEntities = new List<IFCEntity>();
               visitedEntities.Add(this);
               if (!HasValidSubElementGeometry(visitedEntities))
               {
                  // We will not warn if this is an IfcSpatialStructureElement; those aren't expected to have their own geometry.
                  Importer.TheLog.LogWarning(Id, "There is no valid geometry for this " + EntityType.ToString() + "; entity will not be built.", false);
               }
            }
         }

         base.Create(doc);
      }

      /// <summary>
      /// Processes object placement of the product.
      /// </summary>
      /// <param name="ifcProduct">The IfcProduct handle.</param>
      protected void ProcessObjectPlacement(IFCAnyHandle ifcProduct)
      {
         IFCAnyHandle objectPlacement = IFCAnyHandleUtil.GetInstanceAttribute(ifcProduct, "ObjectPlacement");

         if (!IFCAnyHandleUtil.IsNullOrHasNoValue(objectPlacement))
         {
            ObjectLocation = IFCLocation.ProcessIFCObjectPlacement(objectPlacement);
            AddLocationToPlacementBoundingBox();
            IFCSite.CheckObjectPlacementIsRelativeToSite(this, ifcProduct.StepId, objectPlacement);
         }
      }

      /// <summary>
      /// Processes an IfcProduct object.
      /// </summary>
      /// <param name="ifcProduct">The IfcProduct handle.</param>
      /// <returns>The IFCProduct object.</returns>
      public static IFCProduct ProcessIFCProduct(IFCAnyHandle ifcProduct)
      {
         if (IFCAnyHandleUtil.IsNullOrHasNoValue(ifcProduct))
         {
            Importer.TheLog.LogNullError(IFCEntityType.IfcProduct);
            return null;
         }

         try
         {
            IFCEntity cachedProduct;
            if (IFCImportFile.TheFile.EntityMap.TryGetValue(ifcProduct.StepId, out cachedProduct))
               return (cachedProduct as IFCProduct);

            if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcProduct, IFCEntityType.IfcElement))
               return IFCElement.ProcessIFCElement(ifcProduct);

            if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcProduct, IFCEntityType.IfcGrid))
               return IFCGrid.ProcessIFCGrid(ifcProduct);

            if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcProduct, IFCEntityType.IfcProxy))
               return IFCProxy.ProcessIFCProxy(ifcProduct);

            if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcProduct, IFCEntityType.IfcDistributionPort))
               return IFCDistributionPort.ProcessIFCDistributionPort(ifcProduct);

            if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcProduct, IFCEntityType.IfcAnnotation))
               return IFCAnnotation.ProcessIFCAnnotation(ifcProduct);

            if (IFCImportFile.TheFile.SchemaVersionAtLeast(Enums.IFCSchemaVersion.IFC4Obsolete))
            {
               if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcProduct, IFCEntityType.IfcSpatialElement))
                  return IFCSpatialElement.ProcessIFCSpatialElement(ifcProduct);
            }
            else
            {
               if (IFCAnyHandleUtil.IsValidSubTypeOf(ifcProduct, IFCEntityType.IfcSpatialStructureElement)) 
                  return IFCSpatialStructureElement.ProcessIFCSpatialStructureElement(ifcProduct);
            }
         }
         catch (Exception ex)
         {
            HandleError(ex.Message, ifcProduct, true); 
            return null;
         }

         Importer.TheLog.LogUnhandledSubTypeError(ifcProduct, IFCEntityType.IfcProduct, false);
         return null;
      }
   }
}