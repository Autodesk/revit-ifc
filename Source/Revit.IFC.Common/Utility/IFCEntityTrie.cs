using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit.IFC.Common.Utility
{
   /// <summary>
   /// A simple implementation of Trie (similar to radix tree, or inverted index) structure for IFC Entity list.
   /// This is used for a fast filter in the search field
   /// </summary>
   public class IFCEntityTrie
   {
      /// <summary>
      /// the "valid" (or relevant) IFC entities are kept in this Dictionary 
      /// </summary>
      public IDictionary<short, string> FilteredIFCEntityDict { get; private set; } = new Dictionary<short, string>();

      private readonly IFCEntityTrieNode m_Root = new IFCEntityTrieNode();

      /// <summary>
      /// IFCEntityTrie Constructor
      /// </summary>
      public IFCEntityTrie()
      {
      }

      /// <summary>
      /// Add the valid/relevant IFC entity into the Dictionary
      /// </summary>
      /// <param name="ifcEntity">the IFC entity string</param>
      public void AddIFCEntityToDict(string ifcEntity)
      {
         FilteredIFCEntityDict.Add((short) FilteredIFCEntityDict.Count, ifcEntity);
      }

      /// <summary>
      /// Add the IFC entity string into the Trie 
      /// </summary>
      /// <param name="word">the IFC entity name</param>
      public void AddEntry(string word)
      {
         for (int wordcharCnt = 0; wordcharCnt < word.Length; wordcharCnt++)
         {
            IFCEntityTrieNode currNode = m_Root;
            for (int cnt = wordcharCnt; cnt < word.Length; cnt++)
            {
               currNode = currNode.GetChild(word[cnt], FilteredIFCEntityDict, true);
               if (currNode == null)
                  break;
            }
         }
      }

      /// <summary>
      /// Get the list of IFC entities given the partial name
      /// </summary>
      /// <param name="partialWord">partial word to search</param>
      /// <returns>the list of IFC entities that contain the partial word</returns>
      public IList<string> PartialWordSearch(string partialWord)
      {
         SortedList<string, string> foundItems = new SortedList<string, string>();
         IFCEntityTrieNode currNode = m_Root;
         for (int cnt = 0; cnt < partialWord.Length; cnt++)
         {
            currNode = currNode.GetChild(partialWord[cnt], FilteredIFCEntityDict);
            if (currNode == null)
               break;
         }

         if (currNode != null)
         {
            foreach (short idx in currNode.IndexItemsWithSubstring)
               foundItems.Add(FilteredIFCEntityDict[idx], null);
         }

         return foundItems.Keys.ToList();
      }

      public string dumpInvertedIndex()
      {
         return dumpInvertedIndex(m_Root);
      }

      string dumpInvertedIndex(IFCEntityTrieNode node)
      {
         string dumpString = null;
         IFCEntityTrieNode currNode = node;
         dumpString += currNode.PrintIndexedItems(FilteredIFCEntityDict);

         if (currNode.Children.Count > 0)
         {
            foreach(IFCEntityTrieNode childNode in currNode.Children)
            {
               dumpString += dumpInvertedIndex(childNode);
            }
         }

         return dumpString;
      }
   }

   public class IFCEntityTrieNode
   {
      private IFCEntityTrieNode Parent = null;
      public IList<IFCEntityTrieNode> Children { get; private set; } = new List<IFCEntityTrieNode>();
      char Data;
      public IList<short> IndexItemsWithSubstring { private set; get; } = new List<short>();

      public IFCEntityTrieNode(char inputChar = ' ', IFCEntityTrieNode parent = null)
      {
         Parent = parent;
         Data = inputChar;
      }

      /// <summary>
      /// Get the child node of the Trie node
      /// </summary>
      /// <param name="inputChar">the input character</param>
      /// <param name="nonABSEntDict">the dict of all valid entities</param>
      /// <param name="createIfNotExist">flag to indicate whether the node is to be created if it does not yet exist</param>
      /// <returns>the Trie node found or created</returns>
      public IFCEntityTrieNode GetChild(char inputChar, IDictionary<short, string> entDict, bool createIfNotExist = false)
      {
         foreach (IFCEntityTrieNode child in Children)
         {
            if (child.Data == char.ToUpperInvariant(inputChar) || child.Data == char.ToLowerInvariant(inputChar))
               return child;
         }

         if (createIfNotExist)
            return CreateChild(inputChar, entDict, this.IndexItemsWithSubstring);

         return null;
      }

      /// <summary>
      /// Create a child node of the Trie
      /// </summary>
      /// <param name="inputChar">the input character</param>
      /// <param name="nonABSEntDict">the dict of all valid entities</param>
      /// <returns>the created Trie node</returns>
      public IFCEntityTrieNode CreateChild(char inputChar, IDictionary<short, string> entDict, IList<short> idxWithSubstring)
      {
         string thePrefix = Prefix();
         thePrefix = thePrefix + inputChar;
         IList<short> idxEntries = new List<short>();

         // At the first node, the list is empty: use the list from the original Dict
         if (this.Parent == null)
            idxWithSubstring = entDict.Keys.ToList();

         // Collect the index with the intended prefix
         foreach (short idx in idxWithSubstring)
         {
            string ent = entDict[idx];
            if (ent.IndexOf(thePrefix, StringComparison.OrdinalIgnoreCase) >= 0)
               idxEntries.Add(idx);
         }

         // Don't create any node if there is no corresponding entry in the dict
         if (idxEntries.Count == 0)
            return null;

         IFCEntityTrieNode child = new IFCEntityTrieNode(inputChar, this);
         child.IndexItemsWithSubstring = idxEntries;
         Children.Add(child);

         return child;
      }

      string Prefix()
      {
         string thePrefix = "" + Data;
         IFCEntityTrieNode currParent = Parent;
         while (currParent != null)
         {
            thePrefix = currParent.Data + thePrefix;
            currParent = currParent.Parent;
         }

         return thePrefix.Trim();
      }

      public string PrintIndexedItems(IDictionary<short, string> entDict)
      {
         string indexedItems = Prefix() + " : ";

         foreach (short idx in IndexItemsWithSubstring)
            indexedItems += entDict[idx] + ",";

         indexedItems += "\n";

         return indexedItems;
      }
   }
}
