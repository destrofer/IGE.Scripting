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
	public class TernaryExecutionNode : ConditionalExecutionNode {
		public TernaryExecutionNode(IExecutionTreeNode parent, int line, int character) : base(parent, line, character) {
		}

		public override bool HasResult { get { return true; } }

		public override ScriptVariableType Process(ScriptParser parser, int level) {
			base.Process(parser, level);

			ScriptVariableType retType1, retType2;
			
			Condition = TypeCastExecutionNode.ImplicitCast(parser, level + 1, ScriptVariableType.Bool, Condition);

			retType1 = TrueNode.Process(parser, level + 1);
			retType2 = FalseNode.Process(parser, level + 1);
			
			if( retType1 != retType2 )
				parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.TrueAndFalseNodesMustBeOfSameValueInTernaryOperation, this, retType1, retType2));
			
			return retType1;
		}
				
		public override dynamic Execute(ScriptContext context) {
			dynamic c = Condition.Execute(context);
			dynamic r;
			if( c ) {
				r = TrueNode.Execute(context);
			}
			else {
				r = FalseNode.Execute(context);
			}
			return r;
		}
	}
}
