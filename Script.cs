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
using System.Collections.Generic;

using IGE.Scripting.ExecutionNodes;
using IGE.Scripting.InternalExceptions;

namespace IGE.Scripting {
	/// <summary>
	/// </summary>
	public class Script {
		public ScriptLexer Code = null; // holds tokenized script code. Used in compiling
		public ScriptEnvironment Environment = null;
		public Dictionary<string, FunctionNode> Functions = new Dictionary<string, FunctionNode>();
		public ExecutionSequence ExecutionRoot = null;
		
		public Script(string scriptText) : this(new ScriptLexer(scriptText)) {
		}
		
		public Script(ScriptLexer lexer) {
			Code = lexer;
		}
		
		public ErrorInfo[] Compile() {
			return Compile(new ScriptParser());
		}
		
		public ErrorInfo[] Compile(ScriptParser parser) {
			parser.Parse(this);
			return parser.Errors.ToArray();
		}
		
		/// <summary>
		/// Fully executes the script. Will return only when script stops.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public object Run(ScriptContext context) {
			try {
				context.Reset();
				ExecutionRoot.Execute(context);
			}
			catch(ScriptExecutionExitException ex) {
				return ex.ExitValue;
			}
			return null;
		}
	}
}
