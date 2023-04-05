//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
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
using ITerminalNode = Antlr4.Runtime.Tree.ITerminalNode;
using IToken = Antlr4.Runtime.IToken;
using TokenStreamRewriter = Antlr4.Runtime.TokenStreamRewriter;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Antlr4.Runtime.Misc;

namespace Revit.IFC.Export.Utility
{
   class NodeProperty
   {
      public object nodePropertyValue { get; set; }
      public object originalNodePropertyValue { get; set; }
      public ForgeTypeId uomTypeId { get; set; } = null;
   }

   class ParamExprListener : ParamExprGrammarBaseListener
   {
      TokenStreamRewriter rewriter;
      //ParamExprGrammarParser parser;
      BufferedTokenStream tokens;
      Antlr4.Runtime.Tree.ParseTreeProperty<NodeProperty> nodeProp = new Antlr4.Runtime.Tree.ParseTreeProperty<NodeProperty>();
      object FinalParameterValue = null;
      bool putBackWS = false;
      bool isUnique = false;

      /// <summary>
      /// Get/set Revit parameter name being processed
      /// </summary>
      public string RevitParameterName { get; set; }

      /// <summary>
      /// Get/set Revit element that owns the parameter being processed
      /// </summary>
      public Element RevitElement { get; set; }

      /// <summary>
      /// Get/Set Unit of Measure for value marked as having uom
      /// </summary>
      public string formattedValue { get; set; } = null;

      /// <summary>
      /// Get the unit type of the value
      /// </summary>
      public ForgeTypeId UnitType { get; private set; } = null;

      // Dictionary to keep information of which parameter that contains the running number. Key is a tuple of Revit_category and parameter name
      static IDictionary<Tuple<string, string>, int> LastRunningNumberCollection = new Dictionary<Tuple<string, string>, int>();

      // Unique paramameter value dictionary. The key: a tuple of parameter name and parameter value
      // This dictionary should be reset at the beginning of export or at the end of export
      static IDictionary<Tuple<string, object>, int> UniqueParameterValue = new Dictionary<Tuple<string, object>, int>();

      /// <summary>
      /// Reset the internal dictionary that keep unique parameter values. This is needed to be done at the end of export process
      /// </summary>
      public static void ResetParamExprInternalDicts()
      {
         UniqueParameterValue.Clear();
         LastRunningNumberCollection.Clear();
      }

      /// <summary>
      /// Value in a raw object value. Application must check the datatype of the value
      /// </summary>
      public object Value { get { return FinalParameterValue; } }

      /// <summary>
      /// Check whether the parameter has a value
      /// </summary>
      public bool HasValue { get { return (FinalParameterValue != null); } }

      /// <summary>
      /// Returning as a string value
      /// </summary>
      public string ValueAsString { get { return FinalParameterValue.ToString(); } }

      /// <summary>
      /// Returning as an integer value (null if cannot be converted into an integer value) 
      /// </summary>
      public int? ValueAsInteger
      {
         get
         {
            try
            {
               int ret = Convert.ToInt32(FinalParameterValue);
               return ret;
            }
            catch (Exception)
            {
               return null;
            }
         }
      }

      /// <summary>
      /// Returning as a double value (null if cannot be converted into a double value)
      /// </summary>
      public double? ValueAsDouble
      {
         get
         {
            try
            {
               double ret = Convert.ToDouble(FinalParameterValue);
               return ret;
            }
            catch (Exception)
            {
               return null;
            }
         }
      }

      /// <summary>
      /// Set NodeProperty
      /// </summary>
      /// <param name="node">the node</param>
      /// <param name="value">value</param>
      public void SetNodePropertyValue(Antlr4.Runtime.Tree.IParseTree node, NodeProperty value)
      {
         nodeProp.Put(node, value);
      }

      /// <summary>
      /// Get the NodeProperty
      /// </summary>
      /// <param name="node">the node</param>
      /// <returns>NodeProperty</returns>
      public NodeProperty GetNodePropertyValue(Antlr4.Runtime.Tree.IParseTree node)
      {
         return nodeProp.Get(node);
      }

      /// <summary>
      /// Instantiating the ParemExprListener
      /// </summary>
      /// <param name="tokens">tokens</param>
      public ParamExprListener(BufferedTokenStream tokens)
      {
         this.tokens = tokens;
         rewriter = new TokenStreamRewriter(tokens);
      }

      /// <summary>
      /// Do this when encounters Terminal
      /// </summary>
      /// <param name="node">the node</param>
      public override void VisitTerminal(ITerminalNode node)
      {
         string nodeName = node.Symbol.Text;
         if (putBackWS && whiteSpaceOnRight(node.Symbol.TokenIndex))
            rewriter.InsertAfter(node.Symbol.StopIndex, " ");
      }

      /// <summary>
      /// Checking whether in the original statement there is a whitespace on the right hand of a token. If there is returns true
      /// - useful to restore the whitespace to get the original statement (or modified one) back
      /// </summary>
      /// <param name="idx">Token index</param>
      /// <returns>true if there is a whitespace</returns>
      bool whiteSpaceOnRight(int idx)
      {
         try
         {
            IList<IToken> WSChannel = this.tokens.GetHiddenTokensToRight(idx, ParamExprGrammarLexer.WHITESPACE);
            if (WSChannel != null)
            {
               IToken ws = WSChannel.First();
               if (ws != null)
               {
                  return true;
               }
            }
            return false;
         }
         catch (ArgumentOutOfRangeException)
         {
            // Ignore?
            return false;
         }
      }

      /// <summary>
      /// Executed upon entering param_expr rule
      /// </summary>
      /// <param name="context">the param_expr context</param>
      public override void EnterParam_expr([NotNull] ParamExprGrammarParser.Param_exprContext context)
      {
         base.EnterParam_expr(context);
         if (context.ChildCount == 4 && context.GetChild(0) is ITerminalNode && context.GetChild(0).GetText().Equals("U", StringComparison.CurrentCultureIgnoreCase))
         {
            // For unique value case, we will follow '<name>', '<name> (#)' with # as a running number. Unless the "formula" contains the RUNNINGNUMBER token
            // Here, we will make sure that we keep track the value to identify uniqueness
            isUnique = true;
         }
         else
            isUnique = false;
      }

      /// <summary>
      /// Executed upon existing the param_expr rule
      /// </summary>
      /// <param name="context">the param_expr context</param>
      public override void ExitParam_expr([NotNull] ParamExprGrammarParser.Param_exprContext context)
      {
         base.ExitParam_expr(context);
         NodeProperty paramExprNodeProp = GetNodePropertyValue(context.expr());
         object parValue = paramExprNodeProp.nodePropertyValue;
         // Unique parameter value can only be "enforced" for a string datatype by appending a running number: <parValue> (#), starting with 2
         if (isUnique && parValue is string)
         {
            Tuple<string, object> key = new Tuple<string, object>(RevitParameterName, parValue);
            int counter = 0;
            if (UniqueParameterValue.TryGetValue(key, out counter))
            {
               counter++;
               parValue = parValue.ToString() + " (" + counter.ToString() + ")";
               UniqueParameterValue[key] = counter;
            }
            else
            {
               UniqueParameterValue.Add(key, 1);
            }
         }
         FinalParameterValue = parValue;
         UnitType = paramExprNodeProp.uomTypeId;
         if (paramExprNodeProp.uomTypeId != null)
         {
            if (FinalParameterValue is double)
            {
               double? paramValueDouble = FinalParameterValue as double?;
               formattedValue = UnitFormatUtils.Format(RevitElement.Document.GetUnits(), paramExprNodeProp.uomTypeId, paramValueDouble.Value, false);
               FinalParameterValue = UnitUtils.ConvertToInternalUnits(paramValueDouble.Value, paramExprNodeProp.uomTypeId);
            }
         }
      }

      /// <summary>
      /// Executed upon exiting atomic_param rule
      /// </summary>
      /// <param name="context">the atomic_param context</param>
      public override void ExitAtomic_param([NotNull] ParamExprGrammarParser.Atomic_paramContext context)
      {
         base.ExitAtomic_param(context);
         NodeProperty nodeP = new NodeProperty();
         nodeP.originalNodePropertyValue = context.GetText();

         // Handle special_param
         if (context.GetChild(0) is ParamExprGrammarParser.Special_paramContext)
         {
            var spParCtx = context.GetChild(0) as ParamExprGrammarParser.Special_paramContext;
            if (spParCtx.ELEMENTID() != null)
            {
               nodeP.nodePropertyValue = RevitElement.Id.Value;
            }
            else if (spParCtx.RUNNINGNUMBER() != null)
            {
               var key = new Tuple<string, string>(RevitElement.Category.Name, RevitParameterName);
               int lastNumber;
               if (LastRunningNumberCollection.ContainsKey(key))
               {
                  lastNumber = ++LastRunningNumberCollection[key];
               }
               else
               {
                  lastNumber = 1;
                  LastRunningNumberCollection.Add(key, lastNumber);
               }
               nodeP.nodePropertyValue = lastNumber;
            }
            else if (spParCtx.RUNNINGNUMBERINSTANCE() != null)
            {
               var key = new Tuple<string, string>(RevitElement.Id.ToString(), null);
               int lastNumber;
               if (LastRunningNumberCollection.ContainsKey(key))
               {
                  lastNumber = ++LastRunningNumberCollection[key];
               }
               else
               {
                  lastNumber = 1;
                  LastRunningNumberCollection.Add(key, lastNumber);
               }
               nodeP.nodePropertyValue = lastNumber;
            }
            //else if (spParCtx.AUTOCALCULATE() != null)
            //{

            //}
            else
            {
               // Not supported
            }
         }
         //  handle objref param_name (',' param_name)
         else
         {
            object parValue = null;
            var thisObj = context.GetChild(0) as ParamExprGrammarParser.ObjrefContext;
            var paramNames = context.param_name();
            if (paramNames.Count() >= 1)
            {
               // This is the first level reference
               if (thisObj.THIS() != null)
                  parValue = GetValueFromParam_nameContext(RevitElement, paramNames[0]);
               else if (thisObj.TYPE() != null)
                  parValue = GetValueFromParam_nameContext(RevitElement.Document.GetElement(RevitElement.GetTypeId()), paramNames[0]);
            }

            if (paramNames.Count() > 1 && parValue != null && parValue is ElementId)
            {
               // Parameter should be obtained from the second level reference if the parameter is of ElementId type
               parValue = GetValueFromParam_nameContext(RevitElement.Document.GetElement(parValue as ElementId), paramNames[1]);
            }
            nodeP.nodePropertyValue = parValue;
         }
         SetNodePropertyValue(context, nodeP);
      }

      /// <summary>
      /// Executed upon exiting expr rule
      /// </summary>
      /// <param name="context">the expr context</param>
      public override void ExitExpr([NotNull] ParamExprGrammarParser.ExprContext context)
      {
         base.ExitExpr(context);
         NodeProperty retExpr = new NodeProperty();

         // value
         if (context.GetChild(0) is ParamExprGrammarParser.ValueContext && context.children.Count == 1)
            retExpr = GetNodePropertyValue(context.GetChild(0));
         // | atomic_param
         else if (context.GetChild(0) is ParamExprGrammarParser.Atomic_paramContext && context.children.Count == 1)
            retExpr = GetNodePropertyValue(context.GetChild(0));
         // | unary_operator expr
         else if (context.GetChild(0) is ParamExprGrammarParser.Unary_operatorContext && context.ChildCount == 2)
            retExpr = ParamExprResolver.ResolveExprUnaryOperator(context.GetChild(0).GetText(), GetNodePropertyValue(context.GetChild(1)));
         // | expr ops expr
         else if (context.GetChild(1) is ParamExprGrammarParser.OpsContext && context.ChildCount == 3)
            retExpr = ParamExprResolver.ResolveExprOpsExpr(context.GetChild(0) as ParamExprGrammarParser.ExprContext, GetNodePropertyValue(context.GetChild(0)),
                                                            context.GetChild(1) as ParamExprGrammarParser.OpsContext, GetNodePropertyValue(context.GetChild(2)));
         // | '(' expr ')' (power_op)?
         else if (context.GetChild(0) is ITerminalNode && context.GetChild(2) is ITerminalNode)
         {
            int powerOp = +1;
            if (context.ChildCount == 4)
               powerOp = ParamExprResolver.GetPowerOp(context.GetChild(3) as ParamExprGrammarParser.Power_opContext);
            retExpr = ParamExprResolver.ResolveExprPowerOp(GetNodePropertyValue(context.GetChild(1) as ParamExprGrammarParser.ExprContext), powerOp);
         }
         else
         {
            // Not valid rule, return nothing? or the original text?
         }

         SetNodePropertyValue(context, retExpr);
      }

      /// <summary>
      /// Executed upon exiting value rule
      /// </summary>
      /// <param name="context">the value context</param>
      public override void ExitValue([NotNull] ParamExprGrammarParser.ValueContext context)
      {
         base.ExitValue(context);
         object value = null;
         ForgeTypeId convertUnit = SpecTypeId.Number;

         string valueStr = context.GetChild(0).GetText();
         if (context.GetChild(0) is ParamExprGrammarParser.StringliteralContext)
         {
            valueStr = valueStr.Trim().Replace("\'", "").Replace("\"", "");
            value = (string)valueStr;
         }
         else if (context.GetChild(0) is ParamExprGrammarParser.RealliteralContext)
         {
            ParamExprGrammarParser.RealliteralContext realCtx = context.GetChild(0) as ParamExprGrammarParser.RealliteralContext;

            valueStr = realCtx.signed_number().GetText();
            int valueInt;
            if (Int32.TryParse(valueStr, out valueInt))
               value = (int)valueInt;
            else
            {
               double valueDbl;
               if (Double.TryParse(valueStr, out valueDbl))
               {
                  value = (double)valueDbl;
               }
            }
         }
         else if (context.GetChild(0) is ParamExprGrammarParser.Value_with_unitContext)
         {
            ParamExprGrammarParser.Value_with_unitContext vwunitCtx = context.GetChild(0) as ParamExprGrammarParser.Value_with_unitContext;
            if (vwunitCtx.UNITTYPE() != null)
            {
               try
               {
                  if (vwunitCtx.GetChild(2) is ParamExprGrammarParser.Atomic_paramContext)
                  {
                     NodeProperty val = GetNodePropertyValue(vwunitCtx.GetChild(2));
                     if (val.nodePropertyValue is double)
                     {
                        value = (double)val.nodePropertyValue;
                     }
                  }
                  else if (vwunitCtx.GetChild(2) is ParamExprGrammarParser.Signed_numberContext)
                  {
                     ParamExprGrammarParser.Signed_numberContext signedNum = vwunitCtx.GetChild(2) as ParamExprGrammarParser.Signed_numberContext;
                     valueStr = signedNum.GetText();
                     double valueDbl;
                     if (Double.TryParse(valueStr, out valueDbl))
                     {
                        value = (double)valueDbl;
                     }
                  }

                  string unitTypeName = vwunitCtx.UNITTYPE().GetText();
                  System.Reflection.PropertyInfo unitType = typeof(Autodesk.Revit.DB.SpecTypeId).GetProperty(unitTypeName);
                  if (unitType != null)
                  {
                     convertUnit = unitType.GetValue(null, null) as ForgeTypeId;
                  }
               }
               catch { }
            }
         }
         NodeProperty nodeP = new NodeProperty();
         nodeP.originalNodePropertyValue = valueStr;
         nodeP.nodePropertyValue = value;
         nodeP.uomTypeId = convertUnit;

         SetNodePropertyValue(context, nodeP);
      }

      object GetParameterValue(Parameter par)
      {
         object parValue = null;
         if (!par.HasValue)
            return parValue;

         switch (par.StorageType)
         {
            case StorageType.Double:
               parValue = par.AsDouble();
               break;
            case StorageType.Integer:
               parValue = par.AsInteger();
               break;
            case StorageType.String:
               parValue = par.AsString();
               break;
            case StorageType.ElementId:
               parValue = par.AsElementId();
               break;
            default:
               break;
         }
         return parValue;
      }

      object GetValueFromParam_nameContext(Element elem, ParamExprGrammarParser.Param_nameContext paramName)
      {
         object parValue = null;
         Element el = elem;
         if (paramName.ChildCount > 1 && paramName.type() != null)
         {
            el = elem.Document.GetElement(RevitElement.GetTypeId());
         }
         parValue = GetParameterValue(el, paramName);
         return parValue;
      }

      object GetParameterValue(Element elem, ParamExprGrammarParser.Param_nameContext paramName)
      {
         object parValue = null;

         string parameterName = paramName.name().GetText().Replace("(", "").Replace(")", "").Replace("'", "").Replace("\"", "");     // Remove the brackets

         // Special parameter (not actual parameter in Revit for Name and UniqueId string)
         if (parameterName.Equals("Name", StringComparison.CurrentCultureIgnoreCase))
         {
            parValue = elem.Name;
         }
         else if (parameterName.Equals("UniqueId", StringComparison.CurrentCultureIgnoreCase))
         {
            parValue = elem.UniqueId;
         }
         else
         {
            var RevitParams = elem.GetParameters(parameterName);
            foreach (Parameter par in RevitParams)
            {
               parValue = GetParameterValue(par);
               break;
            }
         }

         return parValue;
      }
   }
}
