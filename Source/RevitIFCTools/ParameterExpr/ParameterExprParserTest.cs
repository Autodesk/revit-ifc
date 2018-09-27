//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
// Copyright (C) 2016  Autodesk, Inc.
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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using IErrorNode = Antlr4.Runtime.Tree.IErrorNode;
using ITerminalNode = Antlr4.Runtime.Tree.ITerminalNode;
using IToken = Antlr4.Runtime.IToken;
using ParserRuleContext = Antlr4.Runtime.ParserRuleContext;
using Revit.IFC.Export.Utility;

namespace RevitIFCTools.ParameterExpr
{
   class EvalListener : ParamExprGrammarBaseListener
   {
      ParamExprGrammarParser parser;

      public EvalListener(ParamExprGrammarParser parser)
      {
         this.parser = parser;
      }

      public string visitMsg
      {
         set;
         get;
      }

      public override void VisitTerminal(ITerminalNode node)
      {
         //visitMsg = "\t{get node [Token: " + this.parser.TokenNames[node.Symbol.Type] + "] : " + node.Symbol.Text + "}\n";
         string nodeName = node.Symbol.ToString();
         visitMsg = "\t{Visiting node: [" + nodeName + "]\n";
         Logger.writeLog(visitMsg);
      }

      public override void EnterEveryRule(ParserRuleContext context)
      {
         Logger.writeLog("{get rule: " + this.parser.RuleNames[context.RuleIndex] + " : " + context.Start.Text + "}\n");
      }

      public override void ExitEveryRule(ParserRuleContext context)
      {
         Logger.writeLog("{end rule: " + this.parser.RuleNames[context.RuleIndex] + " : " + context.Start.Text + "}\n");
      }
   }
}
