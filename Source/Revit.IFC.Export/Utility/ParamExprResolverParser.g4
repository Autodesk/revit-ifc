grammar ParamExprResolverParser;
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
atomic_param:     ( '{' objref '}'  | '{' atomic_param '}' ) '.' param_name;
objref:           THIS | TYPE | RUNNINGNUMBER ;
expr:				   value
					   | atomic_param
					   | unary_operator expr
					   | expr ops expr
					   | '(' expr ')' (power_op)?
					   ;
param_name:       NAMEWITHSPECIALCHAR_EXPLICIT+;
unary_operator:   '+' | '-' ;
ops:              MULTIPLY | DIVIDE | ADDITION | SUBTRACT ;
INT_DIGIT:    INT;
NAMEWITHSPECIALCHAR_EXPLICIT:    NAMEWITHSPECIALCHAR;
power_op:         '^' ( '-' | '+' )? INT_DIGIT;
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

STRING :		(['] (ESC | .)*? ['])
                        | (["] (ESC | .)*? ["]);
NUMBER:			INT '.' INT? EXP?   // 1.35, 1.35E-9, 0.3
				   | '.' INT EXP?			// .2, .2e-9
				   | INT EXP?            // 1e10
				   | INT                // 45
				   ;
fragment ALPHANUMERIC:	[a-zA-Z0-9_] ;
fragment NAMEWITHSPECIALCHAR:   [a-zA-Z0-9&*%^$#@!_=+,-./"'];

fragment INT:   [0] | [0-9] [0-9]* ; 
fragment EXP:   [Ee] [+\-]? INT ;
fragment ESC:			'\\' (["\\/bfnrt] | UNICODE) ;
fragment UNICODE :		'u' HEX HEX HEX HEX ;
fragment HEX :			[0-9a-fA-F] ;

// NOTE: If WHITESPACE is changed from 1, please also change the following line below:
// WS:				[ \t\n\r]+ -> channel(1) ;
// This is hardwired to 1 (instead of WHITESPACE) to avoid a compiler warning.
WS:						[ \t\n\r]+ -> channel(1) ;