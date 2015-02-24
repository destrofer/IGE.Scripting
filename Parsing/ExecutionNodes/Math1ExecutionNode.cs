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
	public abstract class Math1ExecutionNode : ExecutionTreeNode {
		public IExecutionTreeNode Value = null;
		public ScriptVariableType ValueType = ScriptVariableType.Undefined;

		public override bool HasResult { get { return true; } }
		public virtual bool AffectsVariable { get { return false; } }
		
		public Math1ExecutionNode(IExecutionTreeNode parent, int line, int character) : base(parent, line, character) {
		}

		public override ScriptVariableType Process(ScriptParser parser, int level) {
			base.Process(parser, level);

			ValueType = Value.Process(parser, level + 1);
			
			dynamic a, b;
			a = ValueType.GetSampleValue();
			try {
				b = ApplyOperator(ref a);
				ResultType = ScriptVariableTypeExtension.GetScriptVariableType(b);
			}
			catch(UserFriendlyException) {
				throw;
			}
			catch {
				parser.Errors.Add(new ErrorInfo(ErrorLevel.CriticalError, ErrorCode.OperatorCannotBeApplied1, this, ValueType));
				throw new ScriptCriticalErrorException();
			}
			
			return ValueType;
		}
		
		public override dynamic Execute(ScriptContext context) {
			dynamic v = Value.Execute(context);
			dynamic r = ApplyOperator(ref v);
			if( AffectsVariable && Value is ScriptIdentifierToken ) {
				context.SetVariable(((ScriptIdentifierToken)Value).Value, v);
			}
			return r;
		}
		
		public abstract dynamic ApplyOperator(ref dynamic a);
		
		public override string Output (int level) {
			return Utils.IndentedFormat(level, "{0}\n{1}", this.GetType().Name, Value.Output(level + 1));
		}
	}
}
