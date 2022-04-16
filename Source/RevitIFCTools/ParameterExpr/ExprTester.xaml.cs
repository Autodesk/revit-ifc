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

using System.Windows;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Revit.IFC.Export.Utility;

namespace RevitIFCTools.ParameterExpr
{
   /// <summary>
   /// Interaction logic for ExprTester.xaml
   /// </summary>
   public partial class ExprTester : Window
   {
      public ExprTester()
      {
         InitializeComponent();
      }

      private void button_Parse_Click(object sender, RoutedEventArgs e)
      {
         if (string.IsNullOrEmpty(textBox_Expr.Text))
            return;

         AntlrInputStream input = new AntlrInputStream(textBox_Expr.Text);
         ParamExprGrammarLexer lexer = new ParamExprGrammarLexer(input);
         CommonTokenStream tokens = new CommonTokenStream(lexer);
         ParamExprGrammarParser parser = new ParamExprGrammarParser(tokens);
         parser.RemoveErrorListeners();
         Logger.resetStream();

         parser.AddErrorListener(new ParamExprErrorListener());

         //IParseTree tree = parser.start_rule();
         IParseTree tree = parser.param_expr();
         ParseTreeWalker walker = new ParseTreeWalker();
         EvalListener eval = new EvalListener(parser);

         walker.Walk(eval, tree);

         // BIMRL_output.Text = tree.ToStringTree(parser);
         string toOutput = new string(Logger.getmStreamContent());
         textBox_Output.Text = tree.ToStringTree(parser) + '\n' + toOutput;
      }

      private void button_Close_Click(object sender, RoutedEventArgs e)
      {
         Close();
      }

      private void button_ClearOutput_Click(object sender, RoutedEventArgs e)
      {
         textBox_Output.Clear();
         textBox_Expr.Clear();
      }

      private void button_ClearAll_Click(object sender, RoutedEventArgs e)
      {
         textBox_Output.Clear();
      }
   }
}
