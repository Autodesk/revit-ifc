/*
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
*/

/*
// ANTLR 4 License
// [The BSD License]
// Copyright (c) 2012 Terence Parr and Sam Harwell
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, 
// are permitted provided that the following conditions are met:
// 
// Redistributions of source code must retain the above copyright notice, this list of conditions 
// and the following disclaimer.
// Redistributions in binary form must reproduce the above copyright notice, this list of 
// conditions and the following disclaimer in the documentation and/or other materials provided 
// with the distribution.
// Neither the name of the author nor the names of its contributors may be used to endorse or 
// promote products derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS 
// OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY 
// AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR 
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY 
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR 
// OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
// POSSIBILITY OF SUCH DAMAGE.
*/

grammar ParamExprGrammar;

@header {
   #pragma warning disable 3021
}

@lexer::members
{
// NOTE: If WHITESPACE is changed from 1, please also change the following line below:
// WS:				[ \t\n\r]+ -> channel(1) ;
// This is hardwired to 1 (instead of WHITESPACE) to avoid a compiler warning.
   public const int WHITESPACE = 1;
}

/*
 * Parser Rules
 */
param_expr:       '{' expr '}' | '[Uu]' '{' expr '}';
expr:               value
                  | atomic_param
                  | unary_operator expr
                  | expr ops expr
                  | '(' expr ')' (power_op)?
                  ;
atomic_param:     objref param_name ('.' param_name)          // Support only 1 level nested reference, no need for overly complex reference
                  | special_param;
objref:           THIS | TYPE ;
type:             TYPE;
special_param:    ELEMENTID
                  | RUNNINGNUMBER 
                  | RUNNINGNUMBERINSTANCE ;
//                  | AUTOCALCULATE ;
param_name:       name | type name;
//name:             '(' (ESC | NAMEWITHSPECIALCHAR)+ ')' ;
name:             '(' STRING ')' ;
unary_operator:   '+' | '-' ;
ops:              MULTIPLY | DIVIDE | ADDITION | SUBTRACT ;
power_op:         '^' ( '-' | '+' )? INT;
value:              realliteral | stringliteral | value_with_unit;
value_with_unit:  UNITTYPE '(' atomic_param ')' | UNITTYPE '(' signed_number ')';
stringliteral:		STRING ;
realliteral:		signed_number;
signed_number:	   ( '+' | '-' )? NUMBER ;

/*
 * Lexer rules
*/
THIS:               '$'[Tt][Hh][Ii][Ss];
TYPE:               '$'[Tt][Yy][Pp][Ee];
ELEMENTID:          '$'[Ee][Ll][Ee][Mm][Ee][Nn][Tt][Ii][Dd];
RUNNINGNUMBER:      '#';
RUNNINGNUMBERINSTANCE:  '##';
AUTOCALCULATE:      '$'[Aa][Uu][Tt][Oo] | '$'[Aa][Uu][Tt][Oo][Mm][Aa][Tt][Ii][Cc] ;
UNITTYPE:       CHARONLY+ ;

/* Operators */
MULTIPLY:		'*';
DIVIDE:			'/';
ADDITION:		'+';
SUBTRACT:		'-';

STRING :		(['] (ESC | .)*? ['])
                        | (["] (ESC | .)*? ["]);
NUMBER:			INT '.' INT? EXP?   // 1.35, 1.35E-9, 0.3
				   | '.' INT EXP?			// .2, .2e-9
				   | INT EXP?            // 1e10
				   | INT                // 45
				   ;
INT:    INT_DIGITS; 
fragment ALPHANUMERIC:          [a-zA-Z0-9_] ;
fragment CHARONLY:              [a-zA-Z_];
fragment ESC:			'\\' (["\\/bfnrt] | UNICODE) ;
fragment UNICODE :		'u' HEX HEX HEX HEX ;
fragment HEX :			[0-9a-fA-F] ;
fragment NAMEWITHSPECIALCHAR:   [a-zA-Z0-9&*%^@!_=+/.,\-];
fragment INT_DIGITS:                   [0] | [0-9] [0-9]* ; 
fragment EXP:                   [Ee] [+\-]? INT ; 

// warning AC0177: 'WHITESPACE' is not a recognized channel name
WS:				[ \t\n\r]+ -> channel(1) ;
