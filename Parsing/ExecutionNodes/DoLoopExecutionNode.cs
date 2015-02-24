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

namespace IGE.Scripting.ExecutionNodes {
	/// <summary>
	/// </summary>
	public class DoLoopExecutionNode : ExecutionTreeNode {
		public IExecutionTreeNode Condition = null;
		public IExecutionTreeNode Code = null;

		public DoLoopExecutionNode(IExecutionTreeNode parent, int line, int character) : base(parent, line, character) {
		}

		public override bool HasResult { get { return false; } }

		public override ScriptVariableType Process(ScriptParser parser, int level) {
			base.Process(parser, level);
			if( Condition != null )
				Condition = TypeCastExecutionNode.ImplicitCast(parser, level + 1, ScriptVariableType.Bool, Condition);
			if( Code != null )
				Code.Process(parser, level + 1);
			if( Code == null && Condition == null )
				parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.LoopWithNoConditionNorCode, this));
			return ScriptVariableType.Undefined;
		}
				
		public override dynamic Execute(ScriptContext context) {
			if( Condition == null ) {
				do {
					try { Code.Execute(context); }
					catch(ScriptExecutionContinueException) { }
					catch(ScriptExecutionBreakException) { break; }
				} while(true);
			}
			else {
				if( Code != null ) {
					do {
						try { Code.Execute(context); }
						catch(ScriptExecutionContinueException) { }
						catch(ScriptExecutionBreakException) { break; }
					} while(Condition.Execute(context));
				}
				else {
					while(Condition.Execute(context));
				}
			}
			return null;
		}

		public override string Output (int level) {
			return Utils.IndentedFormat(level, "{0}\n{1}{2}{3}{4}",
			                            this.GetType().Name,
			                            Utils.IndentedFormat(level + 1, "[CODE]\n"),
			                            (Code != null) ? Code.Output(level + 2) : "",
			                            Utils.IndentedFormat(level + 1, "[CONDITION]\n"),
			                            (Condition != null) ? Condition.Output(level + 2) : ""
			                           );
		}
	}
}
