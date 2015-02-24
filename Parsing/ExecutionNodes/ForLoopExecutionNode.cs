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
	public class ForLoopExecutionNode : ExecutionTreeNode {
		public IExecutionTreeNode Init = null;
		public IExecutionTreeNode Condition = null;
		public IExecutionTreeNode Final = null;
		public IExecutionTreeNode Code = null;

		public ForLoopExecutionNode(IExecutionTreeNode parent, int line, int character) : base(parent, line, character) {
		}

		public override bool HasResult { get { return false; } }

		public override ScriptVariableType Process(ScriptParser parser, int level) {
			base.Process(parser, level);
			parser.IncrementScopeLevel(false);
			if( Init != null )
				Init.Process(parser, level + 1);
			if( Condition != null )
				Condition = TypeCastExecutionNode.ImplicitCast(parser, level + 1, ScriptVariableType.Bool, Condition);
			if( Final != null )
				Final.Process(parser, level + 1);
			if( Code != null )
				Code.Process(parser, level + 1);
			parser.DecrementScopeLevel(false);
			return ScriptVariableType.Undefined;
		}
				
		public override dynamic Execute(ScriptContext context) {
			context.IncrementScopeLevel(false);
			try {
				if( Init != null )
					Init.Execute(context);
				
				if( Final == null ) {
					if( Condition == null ) {
						do {
							try { Code.Execute(context); }
							catch(ScriptExecutionContinueException) { }
							catch(ScriptExecutionBreakException) { break; }
						} while(true);
					}
					else {
						if( Code != null ) {
							while(Condition.Execute(context)) {
								try { Code.Execute(context); }
								catch(ScriptExecutionContinueException) { }
								catch(ScriptExecutionBreakException) { break; }
							}
						}
						else {
							while(Condition.Execute(context));
						}
					}
				}
				else {
					if( Condition == null ) {
						do {
							try { Code.Execute(context); }
							catch(ScriptExecutionContinueException) { }
							catch(ScriptExecutionBreakException) { break; }
							Final.Execute(context);
						} while(true);
					}
					else {
						if( Code != null ) {
							while(Condition.Execute(context)) {
								try { Code.Execute(context); }
								catch(ScriptExecutionContinueException) { }
								catch(ScriptExecutionBreakException) { break; }
								Final.Execute(context);
							}
						}
						else {
							while(Condition.Execute(context)) {
								Final.Execute(context);
							}
						}
					}
				}
			}
			finally {
				context.DecrementScopeLevel(false);
			}
			return null;
		}
		
		public override string Output (int level) {
			return Utils.IndentedFormat(level, "{0}\n{1}{2}{3}{4}{5}{6}{7}{8}",
			                            this.GetType().Name,
			                            Utils.IndentedFormat(level + 1, "[INIT]\n"),
			                            (Init != null) ? Init.Output(level + 2) : "",
			                            Utils.IndentedFormat(level + 1, "[CONDITION]\n"),
			                            (Condition != null) ? Condition.Output(level + 2) : "",
			                            Utils.IndentedFormat(level + 1, "[FINAL]\n"),
			                            (Final != null) ? Final.Output(level + 2) : "",
			                            Utils.IndentedFormat(level + 1, "[CODE]\n"),
			                            (Code != null) ? Code.Output(level + 2) : ""
			                           );
		}
	}
}
