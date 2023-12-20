using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExternalService;
using System.Windows.Interop;
using System.Windows;
using Revit.IFC.Common.Utility;

namespace BIM.IFC.Export.UI.IFCEntityTree
{
   public class BrowseIFCEntityServer : IIFCEntityTreeUIServer
   {
      private (bool, string, string) ShowDialogCommon(bool showTypeNodeOnly,
         string preSelectEnt, string preSelectPDef, IFCExternalServiceUIData data)
      {
         EntityTree theTree = new EntityTree(showTypeNodeOnly,
         preSelectEntity: preSelectEnt, preSelectPdef: preSelectPDef);

         bool? ret = theTree.ShowDialog();
         if (ret.HasValue && ret.Value == true)
         {
            string selEntity = theTree.GetSelectedEntity();
            string selPDef = theTree.GetSelectedPredefinedType();
            data.IsReset = theTree.isReset;
            return (true, selEntity, selPDef);
         }

         return (false, null, null);
      }

      /// <summary>
      /// Launches the IFC Entity tree browser.
      /// </summary>
      /// <param name="data">The IFCExternalServiceUIData from the native code.</param>
      /// <returns>The return status of the dialog window.</returns>
      public bool ShowDialog(IFCExternalServiceUIData data)
      {
         HashSet<string> assignedEntities = new HashSet<string>();
         HashSet<string> assignedPredef = new HashSet<string>();
         string preSelectEnt = null;
         string preSelectPDef = null;

         Document document = data.Document;
         if (document == null)
            return false;

         if (document.IsFamilyDocument)
         {
            using (Transaction tr = new Transaction(document))
            {
               try
               {
                  tr.Start("IFC Entity Selection");
               }
               catch { }

               FamilyManager familyManager = document.FamilyManager;
               FamilyType familyType = familyManager.CurrentType;

               FamilyParameter exportTypeAs = familyManager?.get_Parameter(BuiltInParameter.IFC_EXPORT_ELEMENT_TYPE_AS);
               FamilyParameter exportPredefinedTypeAs = familyManager?.get_Parameter(BuiltInParameter.IFC_EXPORT_PREDEFINEDTYPE_TYPE);

               if ((exportTypeAs?.StorageType != StorageType.String) ||
                  (exportPredefinedTypeAs?.StorageType != StorageType.String))
                  return false;

               preSelectEnt = familyType?.AsString(exportTypeAs);
               preSelectPDef = familyType?.AsString(exportPredefinedTypeAs);

               (bool ret, string selEntity, string selPDef) =
                  ShowDialogCommon(true, preSelectEnt, preSelectPDef, data);

               if (ret)
               {
                  if (data.ParamId.Value == (long)BuiltInParameter.IFC_EXPORT_ELEMENT_TYPE_AS)
                  {
                     familyManager.Set(exportPredefinedTypeAs, selPDef);
                     data.SelectedIFCItem = selEntity;
                  }
                  else if (data.ParamId.Value == (long)BuiltInParameter.IFC_EXPORT_PREDEFINEDTYPE_TYPE)
                  {
                     familyManager.Set(exportTypeAs, selEntity);
                     data.SelectedIFCItem = selPDef;
                  }
               }

               if (tr.HasStarted())
                  tr.Commit();

               return true;
            }
         }

         foreach (ElementId elemId in data.GetRevitElementIds())
         {
            Element elem = data.Document.GetElement(elemId);
            if (elem == null)
               continue;

            // Collect existing assignments to the "IFC Export Element* As" and "IFC Export Predefined Type*"
            BuiltInParameter paramName = (elem is ElementType) ? BuiltInParameter.IFC_EXPORT_ELEMENT_TYPE_AS : BuiltInParameter.IFC_EXPORT_ELEMENT_AS;
            string assignedEnt = elem?.get_Parameter(paramName)?.AsString();
            if (string.IsNullOrEmpty(assignedEnt))
               assignedEntities.Add("<null>");
            else
               assignedEntities.Add(assignedEnt);

            BuiltInParameter pdefParName = (elem is ElementType) ? BuiltInParameter.IFC_EXPORT_PREDEFINEDTYPE_TYPE : BuiltInParameter.IFC_EXPORT_PREDEFINEDTYPE;
            string pdefSel = elem?.get_Parameter(pdefParName)?.AsString();
            if (string.IsNullOrEmpty(pdefSel))
               assignedPredef.Add("<null>");
            else
               assignedPredef.Add(pdefSel);
         }

         // Set the entity and predefined type if they are all the same and not null. This is to be used to preselect them in the UI
         if (assignedEntities.Count == 1 && !assignedEntities.First().Equals("<null>"))
            preSelectEnt = assignedEntities.First();

         if (assignedPredef.Count == 1 && !assignedPredef.First().Equals("<null>"))
            preSelectPDef = assignedPredef.First();

         using (Transaction tr = new Transaction(document))
         {
            try
            {
               tr.Start("IFC Entity Selection");
            }
            catch {}

            bool showTypeNodeOnly = data.ParamId.Value == (long)BuiltInParameter.IFC_EXPORT_ELEMENT_TYPE_AS
                                    || data.ParamId.Value == (long)BuiltInParameter.IFC_EXPORT_PREDEFINEDTYPE_TYPE;

            (bool ret, string selEntity, string selPDef) =
               ShowDialogCommon(showTypeNodeOnly, preSelectEnt, preSelectPDef, data);

            if (ret)
            {
               foreach (ElementId elemId in data.GetRevitElementIds())
               {
                  Element elem = data.Document.GetElement(elemId);
                  if (elem == null)
                     continue;

                  if (data.ParamId.Value == (long)BuiltInParameter.IFC_EXPORT_ELEMENT_TYPE_AS
                     || data.ParamId.Value == (long)BuiltInParameter.IFC_EXPORT_ELEMENT_AS)
                  {
                     BuiltInParameter paramName = (elem is ElementType) ? BuiltInParameter.IFC_EXPORT_PREDEFINEDTYPE_TYPE : BuiltInParameter.IFC_EXPORT_PREDEFINEDTYPE;
                     elem?.get_Parameter(paramName)?.Set(selPDef);
                     data.SelectedIFCItem = selEntity;
                  }
                  else if (data.ParamId.Value == (long)BuiltInParameter.IFC_EXPORT_PREDEFINEDTYPE_TYPE
                     || data.ParamId.Value == (long)BuiltInParameter.IFC_EXPORT_PREDEFINEDTYPE)
                  {
                     BuiltInParameter paramName = (elem is ElementType) ? BuiltInParameter.IFC_EXPORT_ELEMENT_TYPE_AS : BuiltInParameter.IFC_EXPORT_ELEMENT_AS;
                     elem?.get_Parameter(paramName)?.Set(selEntity);
                     data.SelectedIFCItem = selPDef;
                  }
               }

               if (tr.HasStarted())
                  tr.Commit();

               return true;
            }
         }
         return false;
      }

      /// <summary>
      /// Get the server Id
      /// </summary>
      /// <returns>the server guid</returns>
      public Guid GetServerId()
      {
         return new Guid("{DB5C5B21-BB95-4520-972D-ED6889A7A543}");
      }

      /// <summary>
      /// Get the service id this server is responding to
      /// </summary>
      /// <returns>the service id</returns>
      public ExternalServiceId GetServiceId()
      {
         return ExternalServices.BuiltInExternalServices.IFCEntityTreeUIService;
      }

      /// <summary>
      /// Get the name of this server
      /// </summary>
      /// <returns></returns>
      public string GetName()
      {
         return "Browse IFCEntity UI";
      }

      /// <summary>
      /// Get the vendor id
      /// </summary>
      /// <returns>the vendor id</returns>
      public string GetVendorId()
      {
         return "ADSK";
      }

      /// <summary>
      /// Get the description of this server
      /// </summary>
      /// <returns>the description</returns>
      public string GetDescription()
      {
         return Properties.Resources.IFCEntitySelection;
      }
   }
}
