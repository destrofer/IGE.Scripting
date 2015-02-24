/*
 * Author: Viacheslav Soroka
 * 
 * This file is part of IGE <https://github.com/destrofer/IGE>.
 * 
 * IGE is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * IGE is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with IGE.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

namespace IGE.Scripting {
	public class ScriptLexer {
		public string ScriptText;
		protected int Position;
		protected int Length;
		protected int Line;
		protected int Symbol;
		protected bool SignInLiteral = true;
		
		protected StringTokenizingDelegate WhitespaceFunc;
		protected StringTokenizingDelegate[] LexerFunctions;
		protected string[] Keywords;

		public ScriptLexer(string scriptText) {
			ScriptText = scriptText;
			Length = scriptText.Length;
			
			Rewind();
			
			Keywords = new string[] {
				"do", "while", "for", 
				"void", "bool", "char", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double", "decimal", "string",
				"continue", "break", "return", "exit",
				"if", "else", "switch", "case", "default"
			};
			
			WhitespaceFunc = SkipWhitespace;
			
			LexerFunctions = new StringTokenizingDelegate[] {
				TryParseLiteral,
				TryParseIdentifier, // includes parsing keywords
				TryParseOperator
			};
		}
		
		public virtual void Rewind() {
			Position = 0;
			Line = 1;
			Symbol = 1;
			SignInLiteral = true;
		}
		
		/// <summary>
		/// Moves parsing pointer to next readable characters after skipping all whitespaces and comments
		/// </summary>
		/// <param name="codeObj">Always returns null</param>
		/// <returns>Number of characters that whitespace and/or comments take up from the current position</returns>
		protected virtual int SkipWhitespace(out ScriptToken codeObj) {
			int state = 0, pos = this.Position, len = this.Length;
			char c;
			codeObj = null;
			
			while( pos < len && state < 10 ) {
				c = this.ScriptText[pos++];
				switch( state ) {
					case 0:
						if( c == '\n' || c == '\r' || c == '\t' || c == ' ' ) break;
						else if( c == '/' ) { state = 1; break; }
						pos--;
						state = 10;
						break;
					
					case 1: // possibly start of a comment
						if( c == '/' ) { state = 2; break; }
						if( c == '*' ) { state = 3; break; }
						pos -= 2;
						state = 10;
						break;
					
					case 2: // comment until end of line
						if( c == '\n' ) { state = 0; }
						break;
					
					case 3: // comment block
						if( c == '*' ) { state = 4; break; }
						break;
					
					case 4: // possibly an end of comment block
						if( c == '/' ) { state = 0; break; }
						state = 3;
						goto case 3;
				}
			}
			
			return pos - this.Position;
		}
		
		protected virtual int TryParseLiteral(out ScriptToken codeObj) {
			ScriptLiteralTokenType ctype = ScriptLiteralTokenType.None;
			ScriptVariableType etype = ScriptVariableType.Undefined;
			int state = 0, pos = this.Position, len = this.Length;
			char c;
			int charCode; // use ushort for char code instead of int?
			
			codeObj = null;
			StringBuilder val = null;
			StringBuilder charCodeHex = null;
			
			while( pos < len && state < 100 ) {
				c = this.ScriptText[pos++];
				switch( state ) {
					case 0: // the very beginning
						if( c == '@' ) { ctype = ScriptLiteralTokenType.String; etype = ScriptVariableType.String; state = 20; break; }
						goto case 1;
						
					case 1: // don't know if it is really a literal
						if( c == 'n' ) { ctype = ScriptLiteralTokenType.Null; etype = ScriptVariableType.String; state = 80; break; }
						if( c == 't' ) { ctype = ScriptLiteralTokenType.Boolean; etype = ScriptVariableType.Bool; state = 90; break; }
						if( c == 'f' ) { ctype = ScriptLiteralTokenType.Boolean; etype = ScriptVariableType.Bool; state = 95; break; }
						if( c == '\'' ) { ctype = ScriptLiteralTokenType.Char; etype = ScriptVariableType.Char; state = 10; break; }
						if( c == '"' ) { ctype = ScriptLiteralTokenType.String; etype = ScriptVariableType.String; val = new StringBuilder(); state = 30; break; }
						if( c == '+' && SignInLiteral ) { ctype = ScriptLiteralTokenType.Decimal; etype = ScriptVariableType.Int; val = new StringBuilder(); state = 41; break; } // possibly a positive number
						if( c == '-' && SignInLiteral ) { ctype = ScriptLiteralTokenType.Decimal; etype = ScriptVariableType.Int; val = new StringBuilder(); val.Append(c); state = 41; break; } // possibly a negative number
						if( c == '.' ) { ctype = ScriptLiteralTokenType.Decimal; etype = ScriptVariableType.Double; val = new StringBuilder(); val.Append(c); state = 43; break; } // possibly starts from a mantissa of a floating point number
						if( c == '0' ) { etype = ScriptVariableType.Int; val = new StringBuilder(); val.Append(c); state = 40; break; }
						if( c >= '1' && c <= '9' ) { ctype = ScriptLiteralTokenType.Decimal; etype = ScriptVariableType.Int; val = new StringBuilder(); val.Append(c); state = 42; break; }
						return 0;
					
					// parsing a character literal
					case 10:
						val = new StringBuilder();
						if( c == '\\' ) { state = 11; break; }
						val.Append(c);
						state = 12;
						break;
					
					case 11:
						if( c == '0' ) { val.Append((char)0); }
						else if( c == 't' ) { val.Append('\t'); }
						else if( c == 'r' ) { val.Append('\r'); }
						else if( c == 'n' ) { val.Append('\n'); }
						else if( c == 'u' ) { charCodeHex = new StringBuilder(); state = 15; break; }
						else if( c == 'x' ) { charCodeHex = new StringBuilder(); state = 17; break; }
						else { val.Append(c); }
						state = 12;
						break;
					
					case 12:
						if( c != '\'' ) return 0;
						state = 100;
						break;
					
					case 15: goto case 18;
					case 16: goto case 18;
					case 17: goto case 18;
					case 18:
						if( (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') ) {
							charCodeHex.Append(c);
							if( ++state > 18 ) {
								state = 12;
								charCode = (int)Convert.ToUInt32(charCodeHex.ToString(), 16);
								val.Append((char)charCode);
								charCodeHex = null;
							}
							break;
						}
						if( charCodeHex.Length > 0 ) {
							charCode = (int)Convert.ToUInt32(charCodeHex.ToString(), 16);
							val.Append((char)charCode);
							charCodeHex = null;
						}
						state = 12;
						goto case 12;
					
					// parsing a string literal without escape sequences
					case 20:
						if( c == '"' ) { val = new StringBuilder(); state = 21; break; }
						return 0;
						
					case 21:
						if( c == '"' ) { state = 22; break; }
						val.Append(c);
						break;
					
					case 22: // two double quoted in a row result in a doublequote added to a string
						if( c == '"' ) { val.Append(c); state = 21; break; }
						pos--;
						state = 100;
						break;
					
					// parsing a string literal with escape sequences
					case 30:
						if( c == '"' ) { state = 100; break; }
						if( c == '\\' ) { state = 31; break; }
						if( c == '\r' || c == '\n' ) return 0; // in C# newlines are not allowed in strings with escape sequences :/
						val.Append(c);
						break;
					
					case 31:
						if( c == '0' ) { val.Append((char)0); }
						else if( c == 't' ) { val.Append('\t'); }
						else if( c == 'r' ) { val.Append('\r'); }
						else if( c == 'n' ) { val.Append('\n'); }
						else if( c == 'u' ) { charCodeHex = new StringBuilder(); state = 35; break; }
						else if( c == 'x' ) { charCodeHex = new StringBuilder(); state = 37; break; }
						else { val.Append(c); }
						state = 30;
						break;
					
					case 35: goto case 38;
					case 36: goto case 38;
					case 37: goto case 38;
					case 38:
						if( (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') ) {
							charCodeHex.Append(c);
							if( ++state > 38 ) {
								state = 30;
								charCode = (int)Convert.ToUInt32(charCodeHex.ToString(), 16);
								val.Append((char)charCode);
								charCodeHex = null;
							}
							break;
						}
						if( charCodeHex.Length > 0 ) {
							charCode = (int)Convert.ToUInt32(charCodeHex.ToString(), 16);
							val.Append((char)charCode);
							charCodeHex = null;
						}
						state = 30;
						goto case 30;
					
					// parsing a numeric value
					case 40: // possibly a hex/bin/octal value (starts with 0)
						if( c == 'x' || c == 'X' ) { ctype = ScriptLiteralTokenType.Hex; val.Clear(); state = 50; break; }
						if( c == 'b' || c == 'B' ) { ctype = ScriptLiteralTokenType.Binary; val.Clear(); state = 60; break; }
						if( c >= '0' && c <= '7' ) { ctype = ScriptLiteralTokenType.Octal; val.Clear(); val.Append(c); state = 70; break; }
						ctype = ScriptLiteralTokenType.Decimal;
						state = 42;
						goto case 42;
					
					case 41: // starts with - or +
						if( c == '.' ) { etype = ScriptVariableType.Double; val.Append(c); state = 43; break; }
						if( c >= '0' && c <= '9' ) { val.Append(c); state = 42; break; }
						return 0;
					
					case 42: // starts with a digit
						if( c == '.' ) { etype = ScriptVariableType.Double; state = 44; break; }
						if( c >= '0' && c <= '9' ) { val.Append(c); break; }
						if( c == 'F' || c == 'f' ) { etype = ScriptVariableType.Float; state = 100; break; }
						if( c == 'D' || c == 'd' ) { etype = ScriptVariableType.Double; state = 100; break; }
						if( c == 'L' || c == 'l' ) { etype = ScriptVariableType.Long; state = 100; break; }
						if( c == 'M' || c == 'm' ) { etype = ScriptVariableType.Decimal; state = 100; break; }
						if( c == 'U' || c == 'u' ) { etype = ScriptVariableType.UInt; state = 79; break; }
						pos--;
						state = 100;
						break;
					
					case 43: // starts with -. or just .
						if( c >= '0' && c <= '9' ) { val.Append(c); state = 45; break; }
						return 0;

					case 44: // first digit of the mantissa
						if( c >= '0' && c <= '9' ) { val.Append('.'); val.Append(c); state = 45; break; }
						pos -= 2;
						state = 100;
						break;
						
					case 45: // mantissa (only numbers allowed)
						if( c >= '0' && c <= '9' ) { val.Append(c); break; }
						if( c == 'F' || c == 'f' ) { etype = ScriptVariableType.Float; state = 100; break; }
						if( c == 'D' || c == 'd' ) { etype = ScriptVariableType.Double; state = 100; break; }
						if( c == 'L' || c == 'l' ) { etype = ScriptVariableType.Long; state = 100; break; }
						if( c == 'M' || c == 'm' ) { etype = ScriptVariableType.Decimal; state = 100; break; }
						if( c == 'U' || c == 'u' ) { etype = ScriptVariableType.UInt; state = 79; break; }
						pos--;
						state = 100;
						break;
					
					// hex value (0x...)
					case 50:
						if( (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') ) { val.Append(c); state = 51; break; }
						ctype = ScriptLiteralTokenType.Decimal;
						pos -= 2;
						state = 100;
						break;
					
					case 51:
						if( (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') ) { val.Append(c); break; }
						if( c == 'L' || c == 'l' ) { etype = ScriptVariableType.Long; state = 100; break; }
						if( c == 'U' || c == 'u' ) { etype = ScriptVariableType.UInt; state = 79; break; }
						pos--;
						state = 100;
						break;
					
					// bin value (0b...)
					case 60:
						if( c == '0' || c == '1' ) { val.Append(c); state = 61; break; }
						ctype = ScriptLiteralTokenType.Decimal;
						pos -= 2;
						state = 100;
						break;
					
					case 61:
						if( c == '0' || c == '1' ) { val.Append(c); break; }
						if( c == 'L' || c == 'l' ) { etype = ScriptVariableType.Long; state = 100; break; }
						if( c == 'U' || c == 'u' ) { etype = ScriptVariableType.UInt; state = 79; break; }
						pos--;
						state = 100;
						break;
					
					// octal value (0...)
					case 70:
						if( c >= '0' && c <= '7' ) { val.Append(c); break; }
						if( c == '8' || c == '9' ) { ctype = ScriptLiteralTokenType.Decimal; val.Append(c); state = 42; break; }
						if( c == '.' ) { ctype = ScriptLiteralTokenType.Decimal; state = 44; break; }
						if( c == 'L' || c == 'l' ) { etype = ScriptVariableType.Long; state = 100; break; }
						if( c == 'U' || c == 'u' ) { etype = ScriptVariableType.UInt; state = 79; break; }
						pos--;
						state = 100;
						break;
						
					case 79: // number suffix started with u or U 
						if( c == 'L' || c == 'l' ) { etype = ScriptVariableType.ULong; state = 100; break; }
						pos--;
						state = 100;
						break;
					
					// null
					case 80:
						if( c == 'u' ) { state = 81; break; }
						return 0;
					case 81:
						if( c == 'l' ) { state = 82; break; }
						return 0;
					case 82:
						if( c == 'l' ) { val = new StringBuilder(); val.Append("null"); state = 100; break; }
						return 0;

					// true
					case 90:
						if( c == 'r' ) { state = 91; break; }
						return 0;
					case 91:
						if( c == 'u' ) { state = 92; break; }
						return 0;
					case 92:
						if( c == 'e' ) { val = new StringBuilder(); val.Append("true"); state = 100; break; }
						return 0;

					// false
					case 95:
						if( c == 'a' ) { state = 96; break; }
						return 0;
					case 96:
						if( c == 'l' ) { state = 97; break; }
						return 0;
					case 97:
						if( c == 's' ) { state = 98; break; }
						return 0;
					case 98:
						if( c == 'e' ) { val = new StringBuilder(); val.Append("false"); state = 100; break; }
						return 0;
						
					default:
						return 0;
				}
			}
			
			if( val == null )
				return 0;
			codeObj = new ScriptLiteralToken(val.ToString(), ctype, etype, Line, Symbol);
			return pos - this.Position;
		}
		
		protected virtual int TryParseIdentifier(out ScriptToken codeObj) {
			int state = 0, pos = this.Position, len = this.Length;
			char c;
			StringBuilder id = null;
			bool checkKeyword = true;
			
			while( pos < len && state < 3 ) {
				c = this.ScriptText[pos++];
				switch( state ) {
					case 0:
						if( c == '@' ) {
							checkKeyword = false;
							state = 1;
							break;
						}
						goto case 1;
						
					case 1:
						if( (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' ) {
							id = new StringBuilder();
							id.Append(c);
							state = 2;
						}
						else
							state = 3;
						break;
						
					case 2:
						if( (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' )
							id.Append(c);
						else {
							pos--;
							state = 3;
						}
						break;
				}
			}
			
			if( id != null ) {
				string strId = id.ToString();
				if( checkKeyword ) {
					foreach( string keyword in this.Keywords ) {
						if( strId.Equals(keyword) ) {
							codeObj = new ScriptKeywordToken(strId, Line, Symbol);
							return pos - this.Position;
						}
					}
				}
				codeObj = new ScriptIdentifierToken(strId, Line, Symbol);
				return pos - this.Position;
			}
			
			codeObj = null;
			return 0;
		}
		
		protected virtual int TryParseOperator(out ScriptToken codeObj) {
			int state = 0, pos = this.Position, len = this.Length;
			char c;
			codeObj = null;
			StringBuilder op = null;
			
			// ~ ! ^ & |			binary operators
			// ^= &= |=				binary operators
			// ^^ && ||				boolean comparison operators
			// == != >= <= > <		comparison operators
			// + - * / %			math operators
			// ++ --				increment/decrement operators
			// << >> <<= >>=		shift operators
			// += -= *= /= %=		math operators
			// ?? ? :				misc operators (can have multiple meanings)
			// { } ; ,				block operators
			// ( )					priority operators
			// . [ ]				access operators
			// =					assignment operators
			
			// ~ : ; , . ( ) [ ] { }	always single character
			// ! * / % = < >			may be of single character or end only with '='
			// + - ?
			// != ^= &= |= += -= *= /= %= == <= >=
			// ++ -- ?? << >> && || ^^
			// <<= >>=
			
			while( pos < len && state < 10 ) {
				c = this.ScriptText[pos++];
				switch( state ) {
					case 0:
						if( c == '~' || c == ':' || c == ';' || c == ','
						|| c == '.' || c == '(' || c == ')' || c == '['
						|| c == ']' || c == '{' || c == '}' )
						{ op = new StringBuilder(); op.Append(c); state = 10; break; }
						
						if( c == '!' || c == '*' || c == '/' || c == '%' || c == '=' )
						{ op = new StringBuilder(); op.Append(c); state = 1; break; }

						if( c == '+' ) { op = new StringBuilder(); op.Append(c); state = 2; break; }
						if( c == '-' ) { op = new StringBuilder(); op.Append(c); state = 3; break; }
						if( c == '?' ) { op = new StringBuilder(); op.Append(c); state = 4; break; }
						if( c == '<' ) { op = new StringBuilder(); op.Append(c); state = 5; break; }
						if( c == '>' ) { op = new StringBuilder(); op.Append(c); state = 6; break; }
						if( c == '&' ) { op = new StringBuilder(); op.Append(c); state = 7; break; }
						if( c == '|' ) { op = new StringBuilder(); op.Append(c); state = 8; break; }
						if( c == '^' ) { op = new StringBuilder(); op.Append(c); state = 9; break; }
						
						state = 10;
						break;
					
					case 1:
						state = 10;
						if( c == '=' ) { op.Append(c); break; }
						pos--;
						break;
					
					case 2:
						if( c == '+' ) { op.Append(c); state = 10; break; }
						goto case 1;
					
					case 3:
						if( c == '-' ) { op.Append(c); state = 10; break; }
						goto case 1;
					
					case 4:
						state = 10;
						if( c == '?' ) { op.Append(c); break; }
						pos--;
						break;
					
					case 5:
						if( c == '<' ) { op.Append(c); state = 1; break; }
						goto case 1;

					case 6:
						if( c == '>' ) { op.Append(c); state = 1; break; }
						goto case 1;

					case 7:
						if( c == '&' ) { op.Append(c); state = 10; break; }
						goto case 1;

					case 8:
						if( c == '|' ) { op.Append(c); state = 10; break; }
						goto case 1;

					case 9:
						if( c == '^' ) { op.Append(c); state = 10; break; }
						goto case 1;
				}
			}
			if( op != null ) {
				codeObj = new ScriptOperatorToken(op.ToString(), Line, Symbol);
				return pos - this.Position;
			}
			return 0;
		}
		
		protected void Advance(int symbols) {
			int i;
			char c;
			for( i = 0; Position < Length && i < symbols; i++ ) {
				c = ScriptText[Position++];
				if( c == '\n' ) {
					Line++;
					Symbol = 1;
				}
				else if( c != '\r' )
					Symbol++;
			}
		}
		
		public ScriptToken GetNextToken() {
			int result;
			
			ScriptToken obj = null;
			
			Advance(WhitespaceFunc(out obj));
			
			foreach(StringTokenizingDelegate parseFunc in LexerFunctions) {
				result = parseFunc(out obj);
				if( result > 0 ) {
					Advance(result);
					SignInLiteral = !(obj is ScriptIdentifierToken)
						&& !(obj is ScriptLiteralToken)
						&& (
							!(obj is ScriptOperatorToken)
							|| (
								!((ScriptOperatorToken)obj).Value.Equals(")")
								&& !((ScriptOperatorToken)obj).Value.Equals("]")
								&& !((ScriptOperatorToken)obj).Value.Equals("++")
								&& !((ScriptOperatorToken)obj).Value.Equals("--")
							)
						);
					return obj;
				}
			}
			return null;
		}
		
		protected bool EndOfScript { get { return Position >= Length; } }
		
		public IEnumerable<ScriptToken> EnumerateTokens() {
			Rewind();
			ScriptToken token;
			while( !EndOfScript )
				if( (token = GetNextToken()) != null )
					yield return token;
				else
					yield break;
		}
	}
	
	public delegate int StringTokenizingDelegate(out ScriptToken codeObj);
}