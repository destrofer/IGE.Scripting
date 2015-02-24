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

namespace IGE.Scripting {
	/// <summary>
	/// </summary>
	public abstract class ExecutionTreeNode : IExecutionTreeNode {
		protected IExecutionTreeNode m_Parent;
		protected int m_Line;
		protected int m_Character;
		protected ScriptVariableType m_ResultType = ScriptVariableType.Undefined;

		public abstract bool HasResult { get; }
		
		public IExecutionTreeNode Parent { get { return m_Parent; } set { m_Parent = value; } }
		public int Line { get { return m_Line; } }
		public int Character { get { return m_Character; } }
		public ScriptVariableType ResultType { get { return m_ResultType; } protected set { m_ResultType = value; } }

		public virtual bool IsLeafNode { get { return false; } }
		
		public ExecutionTreeNode(IExecutionTreeNode parent, int line, int character) {
			m_Parent = parent;
			m_Line =  line;
			m_Character = character;
		}
		
		public virtual ScriptVariableType Process(ScriptParser parser, int level) {
			if( parser.DebugParsing )
				Utils.IndentedOutput(level, "Parse: {0}", GetType().Name);
			return ScriptVariableType.Undefined;
		}
		
		public abstract dynamic Execute(ScriptContext context);
		
		public virtual string Output (int level) {
			return Utils.IndentedFormat(level, "{0}\n", this.GetType().Name);
		}
	}
}
