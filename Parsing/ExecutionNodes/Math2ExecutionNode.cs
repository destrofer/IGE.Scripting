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
	public abstract class Math2ExecutionNode : ExecutionTreeNode {
		public IExecutionTreeNode LValue = null;
		public IExecutionTreeNode RValue = null;
		public ScriptVariableType LType = ScriptVariableType.Undefined;
		public ScriptVariableType RType = ScriptVariableType.Undefined;

		public override bool HasResult { get { return true; } }
		
		public Math2ExecutionNode(IExecutionTreeNode parent, int line, int character) : base(parent, line, character) {
		}

		public override ScriptVariableType Process(ScriptParser parser, int level) {
			base.Process(parser, level);

			bool hadErrors = false;
			
			try { LType = LValue.Process(parser, level + 1); }
			catch(ScriptCriticalErrorException) { hadErrors = true; }

			try { RType = RValue.Process(parser, level + 1); }
			catch(ScriptCriticalErrorException) { hadErrors = true; }
			
			if( hadErrors )
				throw new ScriptCriticalErrorException(); // we are not going to try the operator because one of nodes had an error
			
			dynamic a, b, c;
			a = LType.GetSampleValue();
			b = RType.GetSampleValue();
			try {
				c = ApplyOperator(a, b);
				ResultType = ScriptVariableTypeExtension.GetScriptVariableType(c);
			}
			catch(UserFriendlyException) {
				throw;
			}
			catch {
				parser.Errors.Add(new ErrorInfo(ErrorLevel.CriticalError, ErrorCode.OperatorCannotBeApplied2, this, LType, RType));
				throw new ScriptCriticalErrorException();
			}
			
			return ResultType;
		}
		
		public override dynamic Execute(ScriptContext context) {
			dynamic a = LValue.Execute(context);
			dynamic b = RValue.Execute(context);
			dynamic r = ApplyOperator(a, b);
			return r;
		}
		
		public abstract dynamic ApplyOperator(dynamic a, dynamic b);
		
		public override string Output (int level) {
			return Utils.IndentedFormat(level, "{0}\n{1}{2}", this.GetType().Name, LValue.Output(level + 1), RValue.Output(level + 1));
		}
	}
}
