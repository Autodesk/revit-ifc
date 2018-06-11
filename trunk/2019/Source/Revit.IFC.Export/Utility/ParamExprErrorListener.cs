//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
// Copyright (C) 2017  Autodesk, Inc.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Revit.IFC.Export.Utility
{
   public class ParamExprErrorListener : BaseErrorListener
   {
      /// <summary>
      /// Checking for syntax error
      /// </summary>
      /// <param name="recognizer">recognizer</param>
      /// <param name="offendingSymbol">offending symbol</param>
      /// <param name="line">line number of the statement</param>
      /// <param name="charPositionInLine">position within the line</param>
      /// <param name="msg">message</param>
      /// <param name="e">recognizer exception</param>
      public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
      {
         string stackList = null;
         IList<string> stack = ((Parser)recognizer).GetRuleInvocationStack();
         stack.Reverse();
         for (int i = 0; i < stack.Count(); i++)
         {
            if (i == 0) stackList = "[";
            stackList = stackList + " " + stack[i];
         }
         stackList = stackList + "]";
         ParamExprLogger.writeLog("\t\t-> rule stack: " + stackList + "\n");
         ParamExprLogger.writeLog("\t\t-> line " + line + ":" + charPositionInLine + " at " + offendingSymbol + ": " + msg + "\n");
      }
   }
}
