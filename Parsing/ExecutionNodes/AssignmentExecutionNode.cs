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
	public class AssignmentExecutionNode : ExecutionTreeNode {
		public ScriptIdentifierToken VarName = null;
		public IExecutionTreeNode Value = null;
		
		public AssignmentExecutionNode(IExecutionTreeNode parent, int line, int character) : base(parent, line, character) {
		}
		
		public override bool HasResult { get { return true; } }

		public override ScriptVariableType Process(ScriptParser parser, int level) {
			base.Process(parser, level);

			ResultType = ScriptVariableType.Undefined;
			try {
				ScriptVariableDefinition v = parser.GetVariable(VarName.Value);
				ResultType = v.VarType;
			}
			catch {
				parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.UndefinedVariable, VarName, VarName.Value));
			}
			Value = TypeCastExecutionNode.ImplicitCast(parser, level + 1, ResultType, Value);
			return ResultType;
		}
		
		public override dynamic Execute(ScriptContext context) {
			dynamic v = Value.Execute(context);
			context.SetVariable(VarName.Value, v);
			return v;
		}
		
		public override string Output (int level) {
			if( Value == null )
				return Utils.IndentedFormat(level, "{0}: {1}\n", this.GetType().Name, VarName.Value);
			else
				return Utils.IndentedFormat(level, "{0}: {1}\n{2}", this.GetType().Name, VarName.Value, Value.Output(level + 1));
		}
	}
}
