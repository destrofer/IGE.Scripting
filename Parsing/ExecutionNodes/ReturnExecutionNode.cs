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
	public class ReturnExecutionNode : ExecutionTreeNode {
		public IExecutionTreeNode ReturnValue = null;
		public ScriptVariableType ReturnValueType = ScriptVariableType.Void;
		public bool IsInGlobalScope = false;
		
		public ReturnExecutionNode(IExecutionTreeNode parent, int line, int character) : base(parent, line, character) {
		}
		
		public override bool HasResult { get { return false; } }

		public override ScriptVariableType Process(ScriptParser parser, int level) {
			base.Process(parser, level);

			if( parser.RequiredReturnType != ScriptVariableType.Undefined ) {
				// Within a function
				if( parser.RequiredReturnType == ScriptVariableType.Void && ReturnValue != null )
					parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.FunctionMustNotReturnAValue, this));
				else if( parser.RequiredReturnType != ScriptVariableType.Void ) {
					if( ReturnValue == null )
						parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.FunctionMustReturnAValue, this));
					else {
						ReturnValue = TypeCastExecutionNode.ImplicitCast(parser, level + 1, parser.RequiredReturnType, ReturnValue);
						ReturnValueType = parser.RequiredReturnType;
					}
				}
				IsInGlobalScope = false;
			}
			else {
				// Within the global scope. Anything or nothing may be returned (works same as 'exit').
				if( ReturnValue != null )
					ReturnValueType = ReturnValue.Process(parser, level + 1);
				IsInGlobalScope = true;
			}
			return ScriptVariableType.Undefined;
		}
		
		public override dynamic Execute(ScriptContext context) {
			if( IsInGlobalScope ) {
				if( ReturnValue == null ) {
					throw new ScriptExecutionExitException(null);
				}
				else {
					dynamic r = ReturnValue.Execute(context);
					throw new ScriptExecutionExitException(r);
				}
			}
			if( ReturnValue == null ) {
				throw new ScriptExecutionReturnException(null);
			}
			else {
				dynamic r = ReturnValue.Execute(context);
				throw new ScriptExecutionReturnException(r);
			}
		}
		
		public override string Output (int level) {
			if( ReturnValue == null )
				return Utils.IndentedFormat(level, "{0}\n", this.GetType().Name);
			else
				return Utils.IndentedFormat(level, "{0}\n{1}", this.GetType().Name, ReturnValue.Output(level + 1));
		}
	}
}
