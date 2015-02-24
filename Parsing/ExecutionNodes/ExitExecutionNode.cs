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
	public class ExitExecutionNode : ExecutionTreeNode {
		public IExecutionTreeNode ExitValue = null;
		public ScriptVariableType ExitValueType = ScriptVariableType.Void;
		
		public ExitExecutionNode(IExecutionTreeNode parent, int line, int character) : base(parent, line, character) {
		}

		public override bool HasResult { get { return false; } }

		public override ScriptVariableType Process(ScriptParser parser, int level) {
			base.Process(parser, level);
			if( ExitValue != null )
				// we don't care what will be returned, but prefer int if numeric value is going to be returned
				ExitValueType = ExitValue.Process(parser, level + 1);
			return ScriptVariableType.Undefined;
		}
				
		public override dynamic Execute(ScriptContext context) {
			if( ExitValue == null ) {
				throw new ScriptExecutionExitException(null);
			}
			else {
				dynamic r = ExitValue.Execute(context);
				throw new ScriptExecutionExitException(r);
			}
		}
		
		public override string Output (int level) {
			if( ExitValue == null )
				return Utils.IndentedFormat(level, "{0}\n", this.GetType().Name);
			else
				return Utils.IndentedFormat(level, "{0}\n{1}", this.GetType().Name, ExitValue.Output(level + 1));
		}
	}
}
