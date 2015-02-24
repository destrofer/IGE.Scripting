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
using System.Collections.Generic;

using IGE.Scripting.ExecutionNodes;

namespace IGE.Scripting {
	/// <summary>
	/// </summary>
	public class ScriptParser {
		public bool DebugTokenizing = false;
		public bool DebugTreeBuilding = false;
		public bool DebugParsing = false;
		
		// parsing:
		// block / function =>
		// do / while / for / if / switch / continue / break / return / exit =>
		// math =>
		// assignment =>
		// ternary =>
		// c / (type)x / (x) / !x / ~x / ++x / --x / x() / x++ / x-- / x  
		
		// TODO: ParseForLoop  /  ParseSwitchConditional
		
		protected Script m_Script = null;
		
		protected ScriptToken[] Tokens = null;
		private int LoopLevel = 0;
		public ScopeStack<ScriptVariableDefinition> Variables;
		public ScriptVariableType RequiredReturnType = ScriptVariableType.Undefined;
		
		public List<ErrorInfo> Errors = new List<ErrorInfo>();
		
		private static Dictionary<string, ScriptVariableType> m_TypeKeywords;
		private static MathOperatorDefinition[][] m_MathOperators;
		
		private static string[] m_ExpressionEnding_Colon = new string[] { ":" };
		private static string[] m_ExpressionEnding_Semicolon = new string[] { ";" };
		private static string[] m_ExpressionEnding_Bracket = new string[] { ")" };
		private static string[] m_ExpressionEnding_CommaOrSemicolon = new string[] { ",", ";" };
		private static string[] m_ExpressionEnding_CommaOrBracket = new string[] { ",", ")" };
		
		public Script Script { get { return m_Script; } }
		
		/*
		-"do", -"while", -"for", 
		-"void", -("bool", "char", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double", "decimal", "string"),
		-"continue", -"break", -"return", -"exit",
		-"if", /"else", -"switch", /"case", /"default"
		*/
		
		static ScriptParser() {
			m_TypeKeywords = new Dictionary<string, ScriptVariableType>();
			m_TypeKeywords.Add("void", 		ScriptVariableType.Void);
			m_TypeKeywords.Add("bool", 		ScriptVariableType.Bool);
			m_TypeKeywords.Add("char", 		ScriptVariableType.Char);
			m_TypeKeywords.Add("byte", 		ScriptVariableType.Byte);
			m_TypeKeywords.Add("sbyte", 	ScriptVariableType.SByte);
			m_TypeKeywords.Add("short", 	ScriptVariableType.Short);
			m_TypeKeywords.Add("ushort", 	ScriptVariableType.UShort);
			m_TypeKeywords.Add("int", 		ScriptVariableType.Int);
			m_TypeKeywords.Add("uint", 		ScriptVariableType.UInt);
			m_TypeKeywords.Add("long", 		ScriptVariableType.Long);
			m_TypeKeywords.Add("ulong", 	ScriptVariableType.ULong);
			m_TypeKeywords.Add("float", 	ScriptVariableType.Float);
			m_TypeKeywords.Add("double", 	ScriptVariableType.Double);
			m_TypeKeywords.Add("decimal", 	ScriptVariableType.Decimal);
			m_TypeKeywords.Add("string", 	ScriptVariableType.String);
			
			m_MathOperators = new MathOperatorDefinition[][] { // math operators sorted by priority
				// bool only
				new MathOperatorDefinition[] { new MathOperatorDefinition("||", typeof(BooleanOrExecutionNode)) },
				new MathOperatorDefinition[] { new MathOperatorDefinition("^^", typeof(BooleanXorExecutionNode)) },
				new MathOperatorDefinition[] { new MathOperatorDefinition("&&", typeof(BooleanAndExecutionNode)) },
				new MathOperatorDefinition[] { new MathOperatorDefinition("!=", typeof(CmpNEExecutionNode)), new MathOperatorDefinition("==", typeof(CmpEExecutionNode)) },
				new MathOperatorDefinition[] { new MathOperatorDefinition("<", typeof(CmpLExecutionNode)), new MathOperatorDefinition(">", typeof(CmpGExecutionNode)), new MathOperatorDefinition("<=", typeof(CmpLEExecutionNode)), new MathOperatorDefinition(">=", typeof(CmpGEExecutionNode)) },
				// integer only
				new MathOperatorDefinition[] { new MathOperatorDefinition("|", typeof(BinaryOrExecutionNode)) },
				new MathOperatorDefinition[] { new MathOperatorDefinition("^", typeof(BinaryXorExecutionNode)) },
				new MathOperatorDefinition[] { new MathOperatorDefinition("&", typeof(BinaryAndExecutionNode)) },
				new MathOperatorDefinition[] { new MathOperatorDefinition("<<", typeof(ShlExecutionNode)), new MathOperatorDefinition(">>", typeof(ShrExecutionNode)) },
				// any type except boolean
				new MathOperatorDefinition[] { new MathOperatorDefinition("-", typeof(SubtractExecutionNode)), new MathOperatorDefinition("+", typeof(AddExecutionNode)) },
				new MathOperatorDefinition[] { new MathOperatorDefinition("*", typeof(MultiplyExecutionNode)), new MathOperatorDefinition("/", typeof(DivideExecutionNode)), new MathOperatorDefinition("%", typeof(RemainderExecutionNode)) },
			};
		}
		
		public ScriptParser() {
		}
		
		public void Parse(Script script) {
			if( script == null )
				throw new UserFriendlyException("Script is required", "There was an error in scripting engine");
			
			if( DebugTokenizing )
				Console.WriteLine("== Tokenizing START ===");
			m_Script = script;
			Tokens = (new List<ScriptToken>(script.Code.EnumerateTokens())).ToArray();
			Variables = new ScopeStack<ScriptVariableDefinition>();
			if( DebugTokenizing )
				Console.WriteLine("== Tokenizing END ===");
			
			if( DebugTreeBuilding )
				Console.WriteLine("== Building tree START ===");
			int position = 0;
			try {
				IExecutionTreeNode rootNode;
				if( !ParseCodeBlock(null, 0, ref position, out rootNode) )
					rootNode = null;
				m_Script.ExecutionRoot = rootNode as ExecutionSequence;
			}
			catch( ScriptParsingException ex ) {
				Console.WriteLine("{0}", ex.ToString());
			}
			if( DebugTreeBuilding )
				Console.WriteLine("== Building tree END ===");
			
			if( m_Script.ExecutionRoot != null ) {
				if( DebugParsing )
					Console.WriteLine("== Parsing START ===");
				IncrementScopeLevel(true);
				m_Script.ExecutionRoot.Process(this, 0);
				foreach( FunctionNode func in m_Script.Functions.Values )
					func.Process(this, 0);
				DecrementScopeLevel(true);
				if( DebugParsing )
					Console.WriteLine("== Parsing END ===");
			}
		}
		
		protected bool ParseCodeBlock(IExecutionTreeNode parent, int level, ref int position, out IExecutionTreeNode treeNode) {
			// level 0 is for root execution node which may contain functions
			// also root node does not require openning with '{' and closing with '}'
			// other levels may not contain functions
			if( DebugTreeBuilding )
				Utils.IndentedOutput(level, "ParseCodeBlock ({0})", position);
			
			int pos = position;
			ScriptToken token;
			IExecutionTreeNode child;
			string expressionEnding;

			if( EOS(pos) )
				treeNode = new ExecutionSequence(parent, 0, 0);
			else {
				token = GetToken(pos);
				treeNode = new ExecutionSequence(parent, token.Line, token.Character);
			}
			
			for( ; !EOS(pos); pos++ ) {
				token = GetToken(pos);
				
				if( level > 0 ) {
					if( pos == position ) {
						if( token is ScriptOperatorToken && token.Value.Equals("{") )
							continue;
						return false;
					}
				}

				if( token is ScriptOperatorToken ) {
					if( token.Value.Equals("}") ) {
						if( level > 0 ) {
							position = pos + 1;
							return true;
						}
						throw new ScriptParsingException(String.Format("Unexpected end of code block at character {0} on line {1}", token.Character, token.Line));
					}
				}
				
				expressionEnding = ParseExecutableCode(treeNode, level + 1, ref pos, out child, true, m_ExpressionEnding_Semicolon);
				
				pos--; // it will be incremented by cycle, but it is already at next position so we rewind it back a bit
				
				if( child != null ) { // null means "nop", which may be returned by ParseExpression()
					if( child is FunctionNode )
						AddFunction((FunctionNode)child);
					else
						((ExecutionSequence)treeNode).AddNode(child);
				}
			}
			
			if( level > 0 )
				throw new ScriptParsingException("Unexpected end of script");
			
			position = pos;
			return true;
		}

		protected string ParseExecutableCode(IExecutionTreeNode treeNode, int level, ref int pos, out IExecutionTreeNode child, bool parseFunctions, string[] expectedExpressionEnding) {
			string expressionEnding = null;
			if( !ParseCodeBlock(treeNode, level, ref pos, out child) )
			if( !ParseDoLoop(treeNode, level, ref pos, out child) )
			if( !ParseWhileLoop(treeNode, level, ref pos, out child) )
			if( !ParseForLoop(treeNode, level, ref pos, out child) )
			if( !ParseIfConditional(treeNode, level, ref pos, out child) )
			if( !ParseSwitchConditional(treeNode, level, ref pos, out child) )
			if( !(LoopLevel > 0 && ParseLoopControl(treeNode, level, ref pos, out child)) ) // continue, break
			if( !ParseExecutionControl(treeNode, level, ref pos, out child) ) // return, exit
			if( !(parseFunctions && ParseFunction(treeNode, level, ref pos, out child)) )
			if( !ParseVarDefinition(treeNode, level, ref pos, out child) )
				ParseExpression(treeNode, level, ref pos, out child, true, expectedExpressionEnding, out expressionEnding); // expression is the last line of defense. it will throw an exception if it could not be parsed or did not end with one of expected endings
			return expressionEnding;
		}
		
		protected bool ParseDoLoop(IExecutionTreeNode parent, int level, ref int position, out IExecutionTreeNode treeNode) {
			if( DebugTreeBuilding )
				Utils.IndentedOutput(level, "ParseDoLoop ({0})", position);

			int pos = position;
			treeNode = null;
			
			ScriptToken token = GetToken(pos++);
			if( !(token is ScriptKeywordToken && token.Value.Equals("do") ) )
				return false;
			
			string ending;
			DoLoopExecutionNode loop = new DoLoopExecutionNode(parent, token.Line, token.Character);
			treeNode = loop;
			
			LoopLevel++;
			ending = ParseExecutableCode(treeNode, level + 1, ref pos, out loop.Code, false, m_ExpressionEnding_Semicolon);
			LoopLevel--;
			
			token = GetToken(pos++);
			if( !(token is ScriptKeywordToken && token.Value.Equals("while")) )
				throw new ScriptParsingException(String.Format("'while' is expected at character {0} on line {1}", token.Character, token.Line));

			token = GetToken(pos++);
			if( !(token is ScriptOperatorToken && token.Value.Equals("(")) )
				throw new ScriptParsingException(String.Format("'(' is expected at character {0} on line {1}", token.Character, token.Line));
			
			ParseExpression(treeNode, level + 1, ref pos, out loop.Condition, false, m_ExpressionEnding_Bracket, out ending);
			
			position = pos;
			return true;
		}

		protected bool ParseWhileLoop(IExecutionTreeNode parent, int level, ref int position, out IExecutionTreeNode treeNode) {
			if( DebugTreeBuilding )
				Utils.IndentedOutput(level, "ParseWhileLoop ({0})", position);

			int pos = position;
			string ending;
			treeNode = null;
			
			ScriptToken token = GetToken(pos++);
			if( !(token is ScriptKeywordToken && token.Value.Equals("while") ) )
				return false;

			WhileLoopExecutionNode loop = new WhileLoopExecutionNode(parent, token.Line, token.Character);
			treeNode = loop;
			
			token = GetToken(pos++);
			if( !(token is ScriptOperatorToken && token.Value.Equals("(")) )
				throw new ScriptParsingException(String.Format("'(' is expected at character {0} on line {1}", token.Character, token.Line));
			
			ParseExpression(treeNode, level + 1, ref pos, out loop.Condition, false, m_ExpressionEnding_Bracket, out ending);

			LoopLevel++;
			ending = ParseExecutableCode(treeNode, level + 1, ref pos, out loop.Code, false, m_ExpressionEnding_Semicolon);
			LoopLevel--;
			
			position = pos;
			return true;
		}

		protected bool ParseForLoop(IExecutionTreeNode parent, int level, ref int position, out IExecutionTreeNode treeNode) {
			if( DebugTreeBuilding )
				Utils.IndentedOutput(level, "ParseForLoop ({0})", position);

			int pos = position;
			string ending;
			IExecutionTreeNode node;
			ExecutionSequence seq;

			treeNode = null;
			
			ScriptToken token = GetToken(pos++);
			if( !(token is ScriptKeywordToken && token.Value.Equals("for") ) )
				return false;

			ForLoopExecutionNode loop = new ForLoopExecutionNode(parent, token.Line, token.Character);
			treeNode = loop;
			
			token = GetToken(pos++);
			if( !(token is ScriptOperatorToken && token.Value.Equals("(")) )
				throw new ScriptParsingException(String.Format("'(' is expected at character {0} on line {1}", token.Character, token.Line));
			
			if( !ParseVarDefinition(loop, level + 1, ref pos, out loop.Init) ) {
				token = GetToken(pos);
				seq = new ExecutionSequence(treeNode, token.Line, token.Character);
				seq.CreateOwnVariableScope = false;
				do {
					ParseExpression(seq, level + 1, ref pos, out node, true, m_ExpressionEnding_CommaOrSemicolon, out ending);
					seq.AddNode(node);
				} while(ending.Equals(","));
				loop.Init = seq.Empty ? null : seq;
			}

			ParseExpression(treeNode, level + 1, ref pos, out loop.Condition, true, m_ExpressionEnding_Semicolon, out ending);
			
			token = GetToken(pos);
			seq = new ExecutionSequence(treeNode, token.Line, token.Character);
			do {
				ParseExpression(seq, level + 1, ref pos, out node, true, m_ExpressionEnding_CommaOrBracket, out ending);
				seq.AddNode(node);
			} while(ending.Equals(","));
			loop.Final = seq.Empty ? null : seq;
			
			LoopLevel++;
			ending = ParseExecutableCode(treeNode, level + 1, ref pos, out loop.Code, false, m_ExpressionEnding_Semicolon);
			LoopLevel--;
			
			position = pos;
			return true;
		}

		protected bool ParseIfConditional(IExecutionTreeNode parent, int level, ref int position, out IExecutionTreeNode treeNode) {
			if( DebugTreeBuilding )
				Utils.IndentedOutput(level, "ParseIfConditional ({0})", position);

			int pos = position;
			treeNode = null;
			
			ScriptToken token = GetToken(pos++);
			if( !(token is ScriptKeywordToken && token.Value.Equals("if") ) )
				return false;

			ConditionalExecutionNode cond = new ConditionalExecutionNode(parent, token.Line, token.Character);
			treeNode = cond;
			
			token = GetToken(pos++);
			if( !(token is ScriptOperatorToken && token.Value.Equals("(") ) )
				throw new ScriptParsingException(String.Format("'(' is expected at character {0} on line {1}", token.Character, token.Line));
			
			string ending;
			
			ParseExpression(treeNode, level + 1, ref pos, out cond.Condition, false, m_ExpressionEnding_Bracket, out ending);

			ending = ParseExecutableCode(treeNode, level + 1, ref pos, out cond.TrueNode, false, m_ExpressionEnding_Semicolon);
			
			if( !EOS(pos) ) {
				token = GetToken(pos);
				if( token is ScriptKeywordToken && token.Value.Equals("else") ) {
					pos++;
					if( !ParseCodeBlock(treeNode, level + 1, ref pos, out cond.FalseNode) )
						ParseExpression(treeNode, level + 1, ref pos, out cond.FalseNode, false, m_ExpressionEnding_Semicolon, out ending);
				}
			}
			
			position = pos;
			return true;
		}

		protected bool ParseSwitchConditional(IExecutionTreeNode parent, int level, ref int position, out IExecutionTreeNode treeNode) {
			treeNode = null;
			return false;
		}
		
		protected bool ParseLoopControl(IExecutionTreeNode parent, int level, ref int position, out IExecutionTreeNode treeNode) {
			if( DebugTreeBuilding )
				Utils.IndentedOutput(level, "ParseLoopControl ({0})", position);

			int pos = position;
			treeNode = null; 
			ScriptToken token;

			// get return type
			token = GetToken(pos++);
			if( !(token is ScriptKeywordToken) )
				return false;

			if( token.Value.Equals("continue") )
				treeNode = new ContinueExecutionNode(parent, token.Line, token.Character);
			else if( token.Value.Equals("break") )
				treeNode = new BreakExecutionNode(parent, token.Line, token.Character);
			else
				return false;
			
			token = GetToken(pos++);
			
			if( !(token is ScriptOperatorToken && token.Value.Equals(";")) )
				throw new ScriptParsingException(String.Format("';' is expected at character {0} on line {1}", token.Character, token.Line));
			
			position = pos;
			return true;
		}

		protected bool ParseExecutionControl(IExecutionTreeNode parent, int level, ref int position, out IExecutionTreeNode treeNode) {
			if( DebugTreeBuilding )
				Utils.IndentedOutput(level, "ParseExecutionControl ({0})", position);

			int pos = position;
			treeNode = null; 
			ScriptToken token;

			// get return type
			token = GetToken(pos++);
			if( !(token is ScriptKeywordToken) )
				return false;

			if( token.Value.Equals("return") )
				treeNode = new ReturnExecutionNode(parent, token.Line, token.Character);
			else if( token.Value.Equals("exit") )
				treeNode = new ExitExecutionNode(parent, token.Line, token.Character);
			else
				return false;
			
			IExecutionTreeNode rv;
			string expressionEnding;
			ParseExpression(treeNode, level + 1, ref pos, out rv, true, m_ExpressionEnding_Semicolon, out expressionEnding);
			
			if( treeNode is ReturnExecutionNode )
				((ReturnExecutionNode)treeNode).ReturnValue = rv;
			else
				((ExitExecutionNode)treeNode).ExitValue = rv;
			
			position = pos;
			return true;
		}

		protected bool ParseFunction(IExecutionTreeNode parent, int level, ref int position, out IExecutionTreeNode treeNode) {
			if( DebugTreeBuilding )
				Utils.IndentedOutput(level, "ParseFunction ({0})", position);

			int pos = position;
			ScriptToken token;
			treeNode = null;

			// get return type
			token = GetToken(pos++);
			if( !(token is ScriptKeywordToken && m_TypeKeywords.ContainsKey(token.Value)) ) {
				return false;
			}
			FunctionNode func = new FunctionNode(parent, token.Line, token.Character);
			treeNode = func;
			func.ReturnType = m_TypeKeywords[token.Value];
			
			// get function identifier
			if( EOS(pos) ) {
				return false;
			}
			token = GetToken(pos++);
			if( !(token is ScriptIdentifierToken) ) {
				if( func.ReturnType == ScriptVariableType.Void ) // "void" type means it MUST be a function
					throw new ScriptParsingException(String.Format("Function identifier expected at character {0} on line {1}", token.Character, token.Line));
				return false;
			}
			func.Name = (ScriptIdentifierToken)token;
			
			// check parameters beginning
			if( EOS(pos) ) {
				return false;
			}
			token = GetToken(pos++);
			if( !(token is ScriptOperatorToken && token.Value.Equals("(")) ) {
				if( func.ReturnType == ScriptVariableType.Void ) // "void" type means it MUST be a function
					throw new ScriptParsingException(String.Format("'(' expected at character {0} on line {1}", token.Character, token.Line));
				return false;
			}
			
			FunctionNode.FunctionParameter param;
			bool expectSeparator = false;
			do {
				// get next parameter
				token = GetToken(pos++);
				if( token is ScriptOperatorToken && token.Value.Equals(")") ) // end of parameters
					break;
				
				// get separator
				if( expectSeparator ) {
					if( !(token is ScriptOperatorToken && token.Value.Equals(",")) ) // parameter separator
						throw new ScriptParsingException(String.Format("',' expected at character {0} on line {1}", token.Character, token.Line));
					token = GetToken(pos++);
				}
				else
					expectSeparator = true;
				
				param = new FunctionNode.FunctionParameter();
				
				if( !(token is ScriptKeywordToken && m_TypeKeywords.ContainsKey(token.Value)) )
					throw new ScriptParsingException(String.Format("Parameter type expected at character {0} on line {1}", token.Character, token.Line));
				param.ParamType = m_TypeKeywords[token.Value];
				if( param.ParamType == ScriptVariableType.Void )
					throw new ScriptParsingException(String.Format("Parameter type cannot be 'void' at character {0} on line {1}", token.Character, token.Line));
				
				// get identifier
				token = GetToken(pos++);
				if( !(token is ScriptIdentifierToken) )
					throw new ScriptParsingException(String.Format("Parameter identifier expected at character {0} on line {1}", token.Character, token.Line));
				param.Name = (ScriptIdentifierToken)token;
				
				// if( !context.AddVariable(new ScriptVariableDefinition(param.ParamType, param.Name)) )
					// throw new ScriptCompilationException(String.Format("Duplicate parameter '{0}' at character {1} on line {2}", token.Value, token.Character, token.Line));

				// get default parameter value
				token = GetToken(pos);
				if( token is ScriptOperatorToken && token.Value.Equals("=") ) {
					pos++;
					param.HasDefaultValue = true;

					token = GetToken(pos++);
					if( !(token is ScriptLiteralToken) )
						throw new ScriptParsingException(String.Format("Only constant value may be set as default for a parameter at character {0} on line {1}", token.Character, token.Line));
					
					// check if default value type is compatible with parameter type
					param.DefaultValue = (ScriptLiteralToken)token;
				}
				
				func.Parameters.Add(param);
			} while(true);
			
			if( !ParseCodeBlock(func, level + 1, ref pos, out func.Body) )
				throw new ScriptParsingException(String.Format("Function body is expected at character {0} on line {1}", token.Character, token.Line));

			position = pos;
			return true;
		}

		protected bool ParseVarDefinition(IExecutionTreeNode parent, int level, ref int position, out IExecutionTreeNode treeNode) {
			if( DebugTreeBuilding )
				Utils.IndentedOutput(level, "ParseVarDefinition ({0})", position);

			int pos = position;
			treeNode = null;
			
			string ending;
			ScriptToken token = GetToken(pos++), token2;
			
			if( token is ScriptOperatorToken && token.Value.Equals(";") ) {
				position = pos;
				return true;
			}
			
			if( !(token is ScriptKeywordToken && m_TypeKeywords.ContainsKey(token.Value)) )
				return false;
			
			if( token.Value.Equals("void") )
				throw new ScriptParsingException(String.Format("Cannot define void variables at character {0} on line {1}", token.Character, token.Line));
			
			treeNode = new VarDefinitionExecutionNode(parent, token.Line, token.Character);
			((VarDefinitionExecutionNode)treeNode).VarType = m_TypeKeywords[token.Value];
			VarDefinitionExecutionNode.Definition varDef;
			
			do {
				token = GetToken(pos++);
				
				if( !(token is ScriptIdentifierToken) )
					throw new ScriptParsingException(String.Format("Variable identifier is expected at character {0} on line {1}", token.Character, token.Line));
				
				varDef = new VarDefinitionExecutionNode.Definition((ScriptIdentifierToken)token, null);
				
				token2 = GetToken(pos++);
				
				if( !(token2 is ScriptOperatorToken) )
					throw new ScriptParsingException(String.Format("'=', ',' or ';' is expected at character {0} on line {1}", token2.Character, token2.Line));
				
				if( token2.Value.Equals("=") ) {
					ParseExpression(treeNode, level + 1, ref pos, out varDef.DefaultValue, false, m_ExpressionEnding_CommaOrSemicolon, out ending);
				}
				else {
					ending = token2.Value;
					
					if( !(ending.Equals(",") || ending.Equals(";")) )
						throw new ScriptParsingException(String.Format("'=', ',' or ';' is expected at character {0} on line {1}", token2.Character, token2.Line));
				}
				
				((VarDefinitionExecutionNode)treeNode).Definitions.Add(varDef);
				// if( !context.AddVariable(new ScriptVariableDefinition(((VarDefinitionExecutionNode)treeNode).VarType, varDef.Name)) )
					// throw new ScriptCompilationException(String.Format("Variable '{0}' is already defined in current scope at character {1} on line {2}", token.Value, token.Character, token.Line));
			} while(ending.Equals(","));
			
			position = pos;
			return true;
		}

		protected void ParseExpression(IExecutionTreeNode parent, int level, ref int position, out IExecutionTreeNode treeNode, bool allowNOP) {
			string tmp;
			ParseExpression(parent, level, ref position, out treeNode, allowNOP, null, out tmp);
		}
		
		protected void ParseExpression(IExecutionTreeNode parent, int level, ref int position, out IExecutionTreeNode treeNode, bool allowNOP, string[] expectedEndingOperators, out string expressionEnding) {
			if( DebugTreeBuilding )
				Utils.IndentedOutput(level, "ParseExpression ({0})", position);

			int pos = position;
			ScriptToken token;
			treeNode = null;
			expressionEnding = null;
			
			if( !ParseMath(parent, level, ref pos, out treeNode, 0) )
				treeNode = null; // NOP
			
			if( treeNode == null && !allowNOP ) {
				token = GetToken(position);
				throw new ScriptParsingException(String.Format("Expression is expected at character {0} on line {1}", token.Character, token.Line));
			}
			
			if( expectedEndingOperators == null ) {
				position = pos;
				return;
			}
			
			token = GetToken(pos);
			if( token is ScriptOperatorToken && InArray(token.Value, expectedEndingOperators) ) {
				pos++;
				expressionEnding = token.Value;
				position = pos;
				return;
			}
				
			StringBuilder err = new StringBuilder();
			for( int i = 0; i < expectedEndingOperators.Length; i++ ) {
				if( treeNode == null || i > 0 ) {
					if( i == expectedEndingOperators.Length - 1 )
						err.Append(" or ");
					else
						err.Append(", ");
				}
				err.Append('\'');
				err.Append(expectedEndingOperators[i]);
				err.Append('\'');
			}
			
			if( treeNode == null )
				throw new ScriptParsingException(String.Format("Expression{0} is expected at character {1} on line {2}", err.ToString(), token.Character, token.Line));
			else
				throw new ScriptParsingException(String.Format("{0} is expected at character {1} on line {2}", err.ToString(), token.Character, token.Line));
		}

		protected bool ParseAssignment(IExecutionTreeNode parent, int level, ref int position, out IExecutionTreeNode treeNode) {
			if( DebugTreeBuilding )
				Utils.IndentedOutput(level, "ParseAssignment ({0})", position);

			int pos = position;
			treeNode = null;

			ScriptToken token = GetToken(pos++);
			
			if( !(token is ScriptIdentifierToken) )
				return false;
			
			ScriptIdentifierToken varName = (ScriptIdentifierToken)token;
			
			if( EOS(pos) )
				return false;
			
			token = GetToken(pos++);

			if( !(token is ScriptOperatorToken) )
				return false;
			
			Math2ExecutionNode extAssignment;
			switch( token.Value ) {
				case "+=": extAssignment = new AddExecutionNode(null, token.Line, token.Character); break;
				case "-=": extAssignment = new SubtractExecutionNode(null, token.Line, token.Character); break;
				case "*=": extAssignment = new MultiplyExecutionNode(null, token.Line, token.Character); break;
				case "/=": extAssignment = new DivideExecutionNode(null, token.Line, token.Character); break;
				case "%=": extAssignment = new RemainderExecutionNode(null, token.Line, token.Character); break;
				case "<<=": extAssignment = new ShlExecutionNode(null, token.Line, token.Character); break;
				case ">>=": extAssignment = new ShrExecutionNode(null, token.Line, token.Character); break;
				case "&=": extAssignment = new BinaryAndExecutionNode(null, token.Line, token.Character); break;
				case "|=": extAssignment = new BinaryOrExecutionNode(null, token.Line, token.Character); break;
				case "^=": extAssignment = new BinaryXorExecutionNode(null, token.Line, token.Character); break;
				case "=": extAssignment = null; break;
				default: return false; 
			}
			
			treeNode = new AssignmentExecutionNode(parent, token.Line, token.Character);
			((AssignmentExecutionNode)treeNode).VarName = varName;
			
			// now we know it's assignment for sure
			
			ParseExpression(treeNode, level + 1, ref pos, out ((AssignmentExecutionNode)treeNode).Value, false);

			if( extAssignment != null ) {
				extAssignment.Parent = treeNode;
				extAssignment.LValue = ((AssignmentExecutionNode)treeNode).VarName;
				extAssignment.RValue = ((AssignmentExecutionNode)treeNode).Value;
				((AssignmentExecutionNode)treeNode).Value = extAssignment;
			}
			
			position = pos;
			return true;
		}
		
		protected bool ParseTernary(IExecutionTreeNode parent, int level, ref int position, out IExecutionTreeNode treeNode) {
			if( DebugTreeBuilding )
				Utils.IndentedOutput(level, "ParseTernary ({0})", position);

			int pos = position;
			treeNode = null;
			
			if( !ParseAssignment(parent, level + 1, ref pos, out treeNode) )
			if( !ParseFactor(parent, level + 1, ref pos, out treeNode) )
				return false;
			
			if( !EOS(pos) && treeNode != null ) {
				ScriptToken token = GetToken(pos);
				
				if( token is ScriptOperatorToken && token.Value.Equals("?") ) {
					pos++;
					
					// it is a ternary
					TernaryExecutionNode tern = new TernaryExecutionNode(parent, token.Line, token.Character);
					treeNode.Parent = tern;
					tern.Condition = treeNode;
					treeNode = tern;
					
					string ending;
					
					ParseExpression(treeNode, level + 1, ref pos, out tern.TrueNode, false, m_ExpressionEnding_Colon, out ending);
	
					ParseExpression(treeNode, level + 1, ref pos, out tern.FalseNode, false);
				}
			}
			
			position = pos;
			return true;
		}

		protected bool ParseMath(IExecutionTreeNode parent, int level, ref int position, out IExecutionTreeNode treeNode, int mathLevel) {
			if( DebugTreeBuilding )
				Utils.IndentedOutput(level, "ParseMath ({0} / {1})", position, mathLevel);

			int pos = position;
			treeNode = null;
			
			if( mathLevel < 0 || mathLevel >= m_MathOperators.Length )
				return ParseTernary(parent, level, ref position, out treeNode);
			
			if( !ParseMath(parent, level + 1, ref pos, out treeNode, mathLevel + 1) )
				return false;
			
			if( !EOS(pos) ) {
				MathOperatorDefinition[] operatorSet = m_MathOperators[mathLevel];
				MathOperatorDefinition op;
				IExecutionTreeNode rValue;
				ScriptToken token = GetToken(pos);
				Math2ExecutionNode mathNode;
				
				while(token is ScriptOperatorToken && FindMathOperator(token.Value, operatorSet, out op)) {
					pos++;
					if( !ParseMath(parent, level + 1, ref pos, out rValue, mathLevel + 1) ) {
						token = GetToken(pos);
						throw new ScriptParsingException(String.Format("Factor is expected at character {0} on line {1}", token.Character, token.Line));
					}
					mathNode = (Math2ExecutionNode)Activator.CreateInstance(op.TreeNodeType, new object[] { parent, token.Line, token.Character });
					mathNode.LValue = treeNode;
					mathNode.RValue = rValue;
					treeNode.Parent = mathNode;
					treeNode = mathNode;
					
					if( EOS(pos) )
						break;
					token = GetToken(pos);
				}
			}
			
			position = pos;
			return true;
		}

		protected bool ParseFactor(IExecutionTreeNode parent, int level, ref int position, out IExecutionTreeNode treeNode) {
			if( DebugTreeBuilding )
				Utils.IndentedOutput(level, "ParseFactor ({0})", position);

			// literal / type cast / priority / negate / invert / call [/ array access] / identifier
			int pos = position;
			string ending;
			treeNode = null;
			
			ScriptToken token = GetToken(pos);
			
			if( token is ScriptLiteralToken ) {
				pos++;
				position = pos;
				treeNode = (ScriptLiteralToken)token;
				treeNode.Parent = parent;
				return true;
			}
			
			if( token is ScriptOperatorToken ) {
				switch( token.Value ) {
					case "++":
						pos++;
						token = GetToken(pos++);
						if( !(token is ScriptIdentifierToken) )
							throw new ScriptParsingException(String.Format("Variable identifier is expected at character {0} on line {1}", token.Character, token.Line));
						
						treeNode = new PreIncrementExecutionNode(parent, token.Line, token.Character);
						((PreIncrementExecutionNode)treeNode).Value = (ScriptIdentifierToken)token;
						((ScriptIdentifierToken)token).Parent = treeNode;
						position = pos;
						return true;

					case "--":
						pos++;
						token = GetToken(pos++);
						if( !(token is ScriptIdentifierToken) )
							throw new ScriptParsingException(String.Format("Variable identifier is expected at character {0} on line {1}", token.Character, token.Line));
						
						treeNode = new PreDecrementExecutionNode(parent, token.Line, token.Character);
						((PreDecrementExecutionNode)treeNode).Value = (ScriptIdentifierToken)token;
						((ScriptIdentifierToken)token).Parent = treeNode;
						
						position = pos;
						return true;
					
					case "(":
						// type cast or priority
						pos++;
						token = GetToken(pos);
						if( token is ScriptKeywordToken && m_TypeKeywords.ContainsKey(token.Value) ) {
							if( token.Value.Equals("void") )
								throw new ScriptParsingException(String.Format("Cannot cast to void at character {0} on line {1}", token.Character, token.Line));
							
							treeNode = new TypeCastExecutionNode(parent, token.Line, token.Character);
							((TypeCastExecutionNode)treeNode).CastToType = m_TypeKeywords[token.Value];
							
							pos++;

							token = GetToken(pos++);
							if( !(token is ScriptOperatorToken && token.Value.Equals(")")) )
								throw new ScriptParsingException(String.Format("')' is expected at character {0} on line {1}", token.Character, token.Line));

							if( !ParseFactor(treeNode, level + 1, ref pos, out ((TypeCastExecutionNode)treeNode).Value) ) {
								token = GetToken(pos);
								throw new ScriptParsingException(String.Format("Factor is expected at character {0} on line {1}", token.Character, token.Line));
							}
							
							position = pos;
							return true;
						}
						
						ParseExpression(parent, level, ref pos, out treeNode, false, m_ExpressionEnding_Bracket, out ending);

						position = pos;
						return true;

					case "!":
						pos++;
						treeNode = new NegateExecutionNode(parent, token.Line, token.Character);
						if( !ParseFactor(treeNode, level + 1, ref pos, out ((NegateExecutionNode)treeNode).Value) ) {
							token = GetToken(pos);
							throw new ScriptParsingException(String.Format("Factor is expected at character {0} on line {1}", token.Character, token.Line));
						}
						position = pos;
						return true;

					case "~":
						pos++;
						treeNode = new InvertExecutionNode(parent, token.Line, token.Character);
						if( !ParseFactor(treeNode, level + 1, ref pos, out ((InvertExecutionNode)treeNode).Value) ) {
							token = GetToken(pos);
							throw new ScriptParsingException(String.Format("Factor is expected at character {0} on line {1}", token.Character, token.Line));
						}
						position = pos;
						return true;
				}
			}
			
			if( !(token is ScriptIdentifierToken) )
				return false;
			pos++;
			
			if( !EOS(pos) ) {
				ScriptToken token2 = GetToken(pos);
				if( token2 is ScriptOperatorToken ) {
					switch( token2.Value ) {
						case "++":
							pos++;
							treeNode = new PostIncrementExecutionNode(parent, token2.Line, token2.Character);
							((PostIncrementExecutionNode)treeNode).Value = (ScriptIdentifierToken)token;
							((ScriptIdentifierToken)token).Parent = treeNode;
							position = pos;
							return true;

						case "--":
							pos++;
							treeNode = new PostDecrementExecutionNode(parent, token2.Line, token2.Character);
							((PostDecrementExecutionNode)treeNode).Value = (ScriptIdentifierToken)token;
							((ScriptIdentifierToken)token).Parent = treeNode;
							position = pos;
							return true;
						
						case "(":
							// this is a function call
							pos++;
							IExecutionTreeNode par;
							CallExecutionNode call = new CallExecutionNode(parent, token.Line, token.Character);
							treeNode = call;
							call.Name = (ScriptIdentifierToken)token;
							do {
								ParseExpression(treeNode, level + 1, ref pos, out par, true, m_ExpressionEnding_CommaOrBracket, out ending);
								if( par != null || call.Parameters.Count != 0 || ending.Equals(",") )
									call.Parameters.Add(par);
							} while(ending.Equals(","));
							position = pos;
							return true;
					}
				}
			}
			
			treeNode = (ScriptIdentifierToken)token;
			treeNode.Parent = parent;
			position = pos;
			return true;
		}
		
		protected bool InArray(string val, string[] arr) {
			foreach( string elem in arr )
				if( elem.Equals(val) )
					return true;
			return false;
		}
		
		protected bool FindMathOperator(string val, MathOperatorDefinition[] operators, out MathOperatorDefinition op) {
			op = default(MathOperatorDefinition);
			foreach( MathOperatorDefinition elem in operators )
				if( elem.Operator.Equals(val) ) {
					op = elem;
					return true;
				}
			return false;
		}

		public ScriptToken GetToken(int position) {
			if( EOS(position) )
				throw new ScriptParsingException("Unexpected end of script");
			return Tokens[position];
		}
		
		/// <summary>
		/// End Of Script
		/// </summary>
		/// <param name="position"></param>
		/// <returns>True if position is outside script bounds</returns>
		public bool EOS(int position) {
			return position >= Tokens.Length;
		}
		
		public void IncrementScopeLevel(bool isRoot) {
			if( DebugParsing )
				Console.WriteLine("--- IncrementScopeLevel({0})", isRoot);
			Variables.IncrementLevel(isRoot);
			if( DebugParsing )
				Variables.Output();
		}
		
		public void DecrementScopeLevel(bool isRoot) {
			if( DebugParsing )
				Console.WriteLine("--- DecrementScopeLevel({0})", isRoot);
			Variables.DecrementLevel(isRoot);
			if( DebugParsing )
				Variables.Output();
		}
		
		public bool AddVariable(ScriptVariableDefinition var) {
			try {
				Variables.Add(var.Name.Value, var);
			}
			catch {
				return false;
			}
			return true;
		}
		
		public bool AddFunction(FunctionNode func) {
			if( m_Script.Functions.ContainsKey(func.Name.Value) )
				return false;
			m_Script.Functions.Add(func.Name.Value, func);
			return true;
		}
		
		public bool GetFunction(string name, out FunctionNode func) {
			return m_Script.Functions.TryGetValue(name, out func);
		}
		
		public bool ContainsVariable(string name) {
			return Variables.ContainsKey(name);
		}
		
		public ScriptVariableDefinition GetVariable(string varName) {
			try {
				return Variables.Get(varName);
			}
			catch {
				throw new Exception("Variable not found");
			}
		}
		
		#region Internal structures
		
		public struct MathOperatorDefinition {
			public string Operator;
			public Type TreeNodeType;
			
			public MathOperatorDefinition(string op, Type treeNodeType) {
				Operator = op;
				TreeNodeType = treeNodeType;
			}
		}
		
		#endregion
	}
}
