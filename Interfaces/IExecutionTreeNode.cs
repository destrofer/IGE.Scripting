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

namespace IGE.Scripting {
	/// <summary>
	/// </summary>
	public interface IExecutionTreeNode {
		IExecutionTreeNode Parent { get; set; }
		int Line { get; }
		int Character { get; }
		
		/// <summary>
		/// (read-only) TRUE if the node never has any child nodes (parameters). For instance it can be a literal or an identifier.
		/// </summary>
		bool IsLeafNode { get; }
		
		/// <summary>
		/// (read-only) TRUE if the node stores it's result in the stack upon execution.
		/// </summary>
		bool HasResult { get; }
		
		// ExecutionResult Execute(int level, out object returnValue);
		string Output (int level);
		
		/// <summary>
		/// Checks the tree node and all it's children for errors. Also creates
		/// implicit type conversions where possible.
		/// When acceptabeResultTypes is ScriptVariableType.Undefined, then
		/// return will be ignored. It means that the node parent does not care
		/// about the result (does not pull the result) and as such this node
		/// must not push it's result into the stack.
		/// Otherwise returned type must be one of acceptableResultTypes. If
		/// node cannot make a result of any acceptable types then it must
		/// register an impossible implicit cast error in the context.
		/// </summary>
		/// <param name="context">Parsing context of the script.</param>
		/// <param name="level">Depth of the recursion.</param>
		/// <param name="acceptableResultTypes">Bit mask of node execution result types that are allowed to be pushed into the stack.</param>
		/// <param name="preferredResultType">Node execution result type that is preferred to be pushed into the stack. If node can it should push result of that type, because it will not require implicit cast.</param>
		/// <param name="requiredReturnType">Type that must be returned by any encountered return statements. Equals to ScriptVariableType.Undefined when the node is not within a function body. When equals to ScriptVariableType.Void it means that return statements must be without a parameter.</param>
		/// <returns>Return the node execution result type that will be pushed into the stack upon node execution.</returns>
		ScriptVariableType Process(ScriptParser parser, int level);
		
		dynamic Execute(ScriptContext context);
	}
	
	/*
	public enum ExecutionResult : byte {
		NoErrors,
		Continue,
		Break,
		Exit,
		Error,
	}
	*/
}
