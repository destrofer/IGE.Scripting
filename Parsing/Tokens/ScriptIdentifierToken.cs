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

using IGE.Scripting.InternalExceptions;

namespace IGE.Scripting {
	public class ScriptIdentifierToken : ScriptToken, IExecutionTreeNode {
		protected IExecutionTreeNode m_Parent = null;
		public IExecutionTreeNode Parent { get { return m_Parent; } set { m_Parent = value; } }

		public bool HasResult { get { return true; } }
		
		public bool IsLeafNode { get { return true; } }

		public ScriptIdentifierToken(string val, int line, int character) : base(val, line, character) {
		}
		
		public virtual ScriptVariableType Process(ScriptParser parser, int level) {
			if( parser.DebugParsing )
				Utils.IndentedOutput(level, "Parse: {0}", GetType().Name);
			
			ScriptVariableType type = ScriptVariableType.Undefined;
			try {
				type = parser.GetVariable(Value).VarType;
			}
			catch {
				parser.Errors.Add(new ErrorInfo(ErrorLevel.CriticalError, ErrorCode.UndefinedVariable, this, Value));
				throw new ScriptCriticalErrorException();
			}
			
			return type;
		}
		
		public virtual dynamic Execute(ScriptContext context) {
			return context.GetVariable(Value);
		}
		
		public virtual string Output(int level) {
			return Utils.IndentedFormat(level, "{0}: {1}\n", this.GetType().Name, this.Value);
		}
	}
}