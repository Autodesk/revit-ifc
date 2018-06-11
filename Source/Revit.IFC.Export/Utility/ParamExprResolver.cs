//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
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
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

namespace Revit.IFC.Export.Utility
{
   class ParamExprResolver
   {
      /// <summary>
      /// Expected paramameter value type enumeration
      /// </summary>
      public enum ExpectedValueEnum
      {
         STRINGVALUE,
         INTVALUE,
         DOUBLEVALUE,
         UNSUPORTEDVALUETYPE
      }

      string paramExprContent;
      static Element _element;
      string _paramName;

      /// <summary>
      /// Instatiation of ParamExprResolver
      /// </summary>
      /// <param name="elem">Revit element</param>
      /// <param name="paramName">parameter name</param>
      /// <param name="paramVal">parameter string value</param>
      public ParamExprResolver(Element elem, string paramName, string paramVal)
      {
         paramExprContent = paramVal;
         _element = elem;
         _paramName = paramName;
      }

      /// <summary>
      /// Get string value from the expr
      /// </summary>
      /// <returns>string value</returns>
      public string GetStringValue()
      {
         object strVal = Process(ExpectedValueEnum.STRINGVALUE);
         if (strVal == null)
            return null;

         string val = strVal.ToString();
         return val;
      }

      /// <summary>
      /// Get the integer value from the expr
      /// </summary>
      /// <returns>the integer value</returns>
      public int? GetIntValue()
      {
         object intVal = Process(ExpectedValueEnum.INTVALUE);
         if (intVal == null)
            return null;

         int val = (int)intVal;
         return val;
      }

      /// <summary>
      /// Get the double value from the expr
      /// </summary>
      /// <returns>the double value</returns>
      public double? GetDoubleValue()
      {
         object dblVal = Process(ExpectedValueEnum.DOUBLEVALUE);
         if (dblVal == null)
            return null;

         double val = (double)dblVal;
         return val;
      }

      object Process(ExpectedValueEnum expectedValueType)
      {
         object val = null;
         AntlrInputStream input = new AntlrInputStream(paramExprContent);
         ParamExprGrammarLexer lexer = new ParamExprGrammarLexer(input);
         CommonTokenStream tokens = new CommonTokenStream(lexer);
         ParamExprGrammarParser parser = new ParamExprGrammarParser(tokens);
         parser.RemoveErrorListeners();
         ParamExprLogger.resetStream();
         parser.AddErrorListener(new ParamExprErrorListener());
         IParseTree tree = parser.param_expr();
         ParseTreeWalker walker = new ParseTreeWalker();
         ParamExprListener eval = new ParamExprListener(tokens);
         eval.RevitElement = _element;
         eval.RevitParameterName = _paramName;

         try
         {
            walker.Walk(eval, tree);
            if (eval.HasValue)
               val = eval.Value;
         }
         catch
         {

         }
         return val;
      }

      /// <summary>
      /// Resolve unary operator for expr
      /// </summary>
      /// <param name="unaryOp">the unary operator</param>
      /// <param name="expr">the expr</param>
      /// <returns>NodeProperty</returns>
      public static NodeProperty ResolveExprUnaryOperator(string unaryOp, NodeProperty expr)
      {
         NodeProperty ret = new NodeProperty();
         ret.originalNodePropertyValue = unaryOp + " " + ret.originalNodePropertyValue;
         if (expr.nodePropertyValue == null)
         {
            return ret;
         }

         if (expr.nodePropertyValue is Int32)
         {
            if (unaryOp.Equals("-"))
               ret.nodePropertyValue = -1 * ((int) expr.nodePropertyValue);
            else
               ret.nodePropertyValue = (int)expr.nodePropertyValue;
         }
         else if (expr.nodePropertyValue is double)
         {
            if (unaryOp.Equals("-"))
               ret.nodePropertyValue = -1 * ((double)expr.nodePropertyValue);
            else
               ret.nodePropertyValue = (double)expr.nodePropertyValue;
         }
         else
         {
            ret.nodePropertyValue = unaryOp + " " + expr.nodePropertyValue.ToString();
         }

         return ret;
      }

      /// <summary>
      /// Resolve "expr operator expr"
      /// </summary>
      /// <param name="expr1Ctx">context of expr 1</param>
      /// <param name="expr1">NodeProperty of expr 1</param>
      /// <param name="ops">operator</param>
      /// <param name="expr2">NodeProperty of expr 2</param>
      /// <returns>NodeProperty</returns>
      public static NodeProperty ResolveExprOpsExpr(ParamExprGrammarParser.ExprContext expr1Ctx, NodeProperty expr1,
                                                     ParamExprGrammarParser.OpsContext ops, NodeProperty expr2)
      {
         NodeProperty ret = new NodeProperty();
         ret.originalNodePropertyValue = expr1.originalNodePropertyValue + " " + ops.GetText() + " " + expr2.originalNodePropertyValue;

         if (expr1.nodePropertyValue == null && expr2.nodePropertyValue == null)
         {
            return ret;
         }

         if (expr1.nodePropertyValue == null)
         {
            // expr1 is null, the ops is undefined, returns only expr2 as it is
            ret.nodePropertyValue = expr2.nodePropertyValue;
            return ret;
         }

         if (expr2.nodePropertyValue == null)
         {
            // expr2 is null, ops is undefined, returns only expr1 as it is
            ret.nodePropertyValue = expr1.nodePropertyValue;
            return ret;
         }

         if (expr1.nodePropertyValue is ElementId || expr2.nodePropertyValue is ElementId)
         {
            // For ElementId to be in this oper, the Name of the Element will be used and the rest will be converted to strings
            string expr1Str = null;
            if (expr1.nodePropertyValue is ElementId)
               expr1Str = _element.Document.GetElement((ElementId)expr1.nodePropertyValue).Name;
            else
               expr1Str = expr1.nodePropertyValue.ToString();

            string expr2Str = null;
            if (expr2.nodePropertyValue is ElementId)
               expr2Str = _element.Document.GetElement((ElementId)expr2.nodePropertyValue).Name;
            else
               expr2Str = expr2.nodePropertyValue.ToString();

            //ret.nodePropertyValue = expr1Str + " " + ops.GetText() + " " + expr2Str;
            ret.nodePropertyValue = expr1Str + expr2Str;
         }
         else if (expr1.nodePropertyValue is string || expr2.nodePropertyValue is string)
         {
            // one of the expr is a string, and therefore the entire expr will returns string
            //ret.nodePropertyValue = expr1.nodePropertyValue.ToString() + " " + ops.GetText() + " " + expr2.nodePropertyValue.ToString();
            ret.nodePropertyValue = expr1.nodePropertyValue.ToString() + expr2.nodePropertyValue.ToString();
         }
         else if (expr1.nodePropertyValue is double || expr2.nodePropertyValue is double)
         {
            if (ops.MULTIPLY() != null)
               ret.nodePropertyValue = (double)expr1.nodePropertyValue * (double)expr2.nodePropertyValue;
            if (ops.DIVIDE() != null)
               ret.nodePropertyValue = (double)expr1.nodePropertyValue / (double)expr2.nodePropertyValue;
            if (ops.ADDITION() != null)
               ret.nodePropertyValue = (double)expr1.nodePropertyValue + (double)expr2.nodePropertyValue;
            if (ops.SUBTRACT() != null)
               ret.nodePropertyValue = (double)expr1.nodePropertyValue - (double)expr2.nodePropertyValue;
         }
         else
         {
            if (ops.MULTIPLY() != null)
               ret.nodePropertyValue = (int)expr1.nodePropertyValue * (int)expr2.nodePropertyValue;
            if (ops.DIVIDE() != null)
               ret.nodePropertyValue = (double) ((int)expr1.nodePropertyValue / (int)expr2.nodePropertyValue);
            if (ops.ADDITION() != null)
               ret.nodePropertyValue = (int)expr1.nodePropertyValue + (int)expr2.nodePropertyValue;
            if (ops.SUBTRACT() != null)
               ret.nodePropertyValue = (int)expr1.nodePropertyValue - (int)expr2.nodePropertyValue;
         }

         return ret;
      }

      /// <summary>
      /// Get Math power operator
      /// </summary>
      /// <param name="powerOp">the power operator</param>
      /// <returns>integer value of the power</returns>
      public static int GetPowerOp(ParamExprGrammarParser.Power_opContext powerOp)
      {
         int powerOpNumber = int.Parse(powerOp.INT().GetText());
         if (powerOp.ChildCount == 3)
         {
            if (powerOp.GetChild(1).GetText().Equals("-"))
               powerOpNumber = -1 * powerOpNumber;
         }
         return powerOpNumber;
      }

      /// <summary>
      /// Resolve "(expr)^power"
      /// </summary>
      /// <param name="expr">the expr NodeProperty</param>
      /// <param name="powerOp">the power operator</param>
      /// <returns>NodeProperty</returns>
      public static NodeProperty ResolveExprPowerOp(NodeProperty expr, int powerOp)
      {
         NodeProperty ret = new NodeProperty();
         ret.originalNodePropertyValue = "(" + expr.originalNodePropertyValue + ")^" + powerOp.ToString();
         if (expr.nodePropertyValue is double || expr.nodePropertyValue is int)
         {
            ret.nodePropertyValue = Math.Pow((double)expr.nodePropertyValue, (double)powerOp);
         }
         return ret;
      }
   }
}