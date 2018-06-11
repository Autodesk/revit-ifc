grammar ParamExprResolverParser;
@header {
#pragma warning disable 3021
}

@lexer::members
{
	public static int WHITESPACE = 1;
	public static int COMMENTS = 2;
}

/*
 * Parser Rules
 */
atomic_param:     ( '{' objref '}'  | '{' atomic_param '}' ) '.' param_name;
objref:           THIS | TYPE | RUNNINGNUMBER ;
expr:				   value
					   | atomic_param
					   | unary_operator expr
					   | expr ops expr
					   | '(' expr ')' (power_op)?
					   ;
param_name:       NAMEWITHSPECIALCHAR+;
unary_operator:   '+' | '-' ;
ops:              MULTIPLY | DIVIDE | ADDITION | SUBTRACT ;
power_op:         '^' ( '-' | '+' )? INT;
value:				realliteral | stringliteral ;
stringliteral:		STRING ;
realliteral:		signed_number ;
signed_number:	   ( '+' | '-' )? NUMBER ;

/*
 * Lexer rules
*/
THIS:    [Tt][Hh][Ii][Ss];
TYPE:    [Tt][Yy][Pp][Ee];
RUNNINGNUMBER: '#';

/* Operators */
MULTIPLY:		'*';
DIVIDE:			'/';
ADDITION:		'+';
SUBTRACT:		'-';

STRING :		   ( . )*? ;
NUMBER:			INT '.' INT? EXP?   // 1.35, 1.35E-9, 0.3
				   | '.' INT EXP?			// .2, .2e-9
				   | INT EXP?            // 1e10
				   | INT                // 45
				   ;
fragment ALPHANUMERIC:	[a-zA-Z0-9_] ;
fragment NAMEWITHSPECIALCHAR:   [a-zA-Z0-9&*%^$#@!_=+-/.,"'];
fragment INT:   [0] | [0-9] [0-9]* ; 
fragment EXP:   [Ee] [+\-]? INT ; 

WS:						[ \t\n\r]+ -> channel(WHITESPACE) ;