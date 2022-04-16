using System.Collections.Generic;

namespace RevitIFCTools
{
   public enum PsetOrQtoSetEnum
   {
      PROPERTYSET,
      QTOSET,
      PREDEFINEDPSET,
      NOTDEFINED
   }

   public enum ItemsInPsetQtoDefs
   {
      PropertySetOrQtoSetDef,
      PropertyOrQtoDefs,
      PropertyOrQtoDef,
      PropertyOrQtoType,
      PsetOrQtoDefinitionAliases,
      PsetOrQtoDefinitionAlias
   }

   public class PsetOrQto
   {
      static Dictionary<PsetOrQtoSetEnum, Dictionary<ItemsInPsetQtoDefs, string>> m_psetOrQtoDefItems = new Dictionary<PsetOrQtoSetEnum, Dictionary<ItemsInPsetQtoDefs, string>>();

      public static Dictionary<PsetOrQtoSetEnum, Dictionary<ItemsInPsetQtoDefs, string>> PsetOrQtoDefItems
      {
         get
         {
            if (m_psetOrQtoDefItems.Count == 0)
            {
               // Adding Pset related keywords
               Dictionary<ItemsInPsetQtoDefs, string> psetItems = new Dictionary<ItemsInPsetQtoDefs, string>();
               psetItems.Add(ItemsInPsetQtoDefs.PropertySetOrQtoSetDef, "PropertySetDef");
               psetItems.Add(ItemsInPsetQtoDefs.PropertyOrQtoDefs, "PropertyDefs");
               psetItems.Add(ItemsInPsetQtoDefs.PropertyOrQtoDef, "PropertyDef");
               psetItems.Add(ItemsInPsetQtoDefs.PropertyOrQtoType, "PropertyType");
               psetItems.Add(ItemsInPsetQtoDefs.PsetOrQtoDefinitionAliases, "PsetDefinitionAliases");
               psetItems.Add(ItemsInPsetQtoDefs.PsetOrQtoDefinitionAlias, "PsetDefinitionAlias");
               m_psetOrQtoDefItems.Add(PsetOrQtoSetEnum.PROPERTYSET, psetItems);

               // Adding QtoSet related keywords
               Dictionary<ItemsInPsetQtoDefs, string> qtoSetItems = new Dictionary<ItemsInPsetQtoDefs, string>();
               qtoSetItems.Add(ItemsInPsetQtoDefs.PropertySetOrQtoSetDef, "QtoSetDef");
               qtoSetItems.Add(ItemsInPsetQtoDefs.PropertyOrQtoDefs, "QtoDefs");
               qtoSetItems.Add(ItemsInPsetQtoDefs.PropertyOrQtoDef, "QtoDef");
               qtoSetItems.Add(ItemsInPsetQtoDefs.PropertyOrQtoType, "QtoType");
               qtoSetItems.Add(ItemsInPsetQtoDefs.PsetOrQtoDefinitionAliases, "QtoDefinitionAliases");
               qtoSetItems.Add(ItemsInPsetQtoDefs.PsetOrQtoDefinitionAlias, "QtoDefinitionAlias");
               m_psetOrQtoDefItems.Add(PsetOrQtoSetEnum.QTOSET, qtoSetItems);
            }
            return m_psetOrQtoDefItems;
         }
      }
   } 
}
