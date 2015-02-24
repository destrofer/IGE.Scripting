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

namespace IGE.Scripting.ExecutionNodes {
	/// <summary>
	/// </summary>
	public class ConditionalExecutionNode : ExecutionTreeNode {
		public IExecutionTreeNode Condition = null;
		public IExecutionTreeNode TrueNode = null;
		public IExecutionTreeNode FalseNode = null;

		public ConditionalExecutionNode(IExecutionTreeNode parent, int line, int character) : base(parent, line, character) {
		}

		public override bool HasResult { get { return false; } }

		public override ScriptVariableType Process(ScriptParser parser, int level) {
			base.Process(parser, level);
			Condition = TypeCastExecutionNode.ImplicitCast(parser, level + 1, ScriptVariableType.Bool, Condition);
			if( TrueNode != null )
				TrueNode.Process(parser, level + 1);
			if( FalseNode != null )
				FalseNode.Process(parser, level + 1);
			return ScriptVariableType.Undefined;
		}
				
		public override dynamic Execute(ScriptContext context) {
			dynamic c = Condition.Execute(context);
			if( c ) {
				if( TrueNode != null )
					TrueNode.Execute(context);
			}
			else {
				if( FalseNode != null )
					FalseNode.Execute(context);
			}
			return null;
		}
		
		public override string Output (int level) {
			return Utils.IndentedFormat(level, "{0}\n{1}{2}{3}", this.GetType().Name, Condition.Output(level + 1), TrueNode.Output(level + 1), FalseNode.Output(level + 1));
		}
	}
}
