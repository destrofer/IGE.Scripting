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
using System.Text;
using System.Collections.Generic;

using IGE.Scripting.InternalExceptions;

namespace IGE.Scripting.ExecutionNodes {
	/// <summary>
	/// </summary>
	public class ExecutionSequence : ExecutionTreeNode {
		protected List<IExecutionTreeNode> m_Children = new List<IExecutionTreeNode>();
		public bool CreateOwnVariableScope = true;
		
		public override bool HasResult { get { return false; } }
		
		public int Count { get { return m_Children.Count; } }
		public bool Empty { get { return Count == 0; } }
		
		public ExecutionSequence(IExecutionTreeNode parent, int line, int character) : base(parent, line, character) {
		}
		
		public void AddNode(IExecutionTreeNode node) {
			if( node != null )
				m_Children.Add(node);
		}
		
		public override ScriptVariableType Process(ScriptParser parser, int level) {
			base.Process(parser, level);

			IExecutionTreeNode node = null;
			if( m_Parent != null && CreateOwnVariableScope)
				parser.IncrementScopeLevel(false);
			
			int i, processCount = 0;
			bool exitFound = false;
			for( i = 0; i < m_Children.Count; i++ ) {
				node = m_Children[i];
				exitFound = (node is ReturnExecutionNode || node is ExitExecutionNode);
				if( exitFound && i < m_Children.Count - 1) {
					parser.Errors.Add(new ErrorInfo(ErrorLevel.Warning, ErrorCode.UnreachableCode, m_Children[i + 1]));
					break;
				}
				processCount++;
			}
			if( level == 0 && !exitFound ) {
				if( m_Parent != null && parser.RequiredReturnType != ScriptVariableType.Undefined && parser.RequiredReturnType != ScriptVariableType.Void ) {
					parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.NotAllCodePathsReturnAValue, this));
					// return;
				}
				if( m_Parent == null && node != null && node.HasResult ) {
					// Not within a function, but at the very end of the script.
					// Make result of the last statement to be returned as an
					// exit data of the script. This is needed to simplify
					// scripts that work as conditions of some scripted objects
					// like dialogues or quests.
					ExitExecutionNode exitNode = new ExitExecutionNode(this, node.Line, node.Character);
					exitNode.ExitValue = node;
					node.Parent = exitNode;
					m_Children[m_Children.Count - 1] = exitNode;
				}
			}
			
			// processing has to be done AFTER virtual exit node is incorporated
			for( i = 0; i < processCount; i++ ) {
				node = m_Children[i];
				
				try { node.Process(parser, level + 1); }
				catch(ScriptCriticalErrorException) {}
				
				if( (node is ReturnExecutionNode || node is ExitExecutionNode) && i < m_Children.Count - 1)
					break;
			}
			
			// Don't decrement scope level for root node to keep global
			// variables in the variable stack. This is required for functions
			// to know what global variables they will have access to while
			// parsing.
			if( m_Parent != null && CreateOwnVariableScope )
				parser.DecrementScopeLevel(false);
			
			return ScriptVariableType.Undefined;
		}
		
		public override dynamic Execute(ScriptContext context) {
			context.IncrementScopeLevel(m_Parent == null);
			try {
				foreach( IExecutionTreeNode node in m_Children )
					node.Execute(context);
			}
			finally {
				context.DecrementScopeLevel(m_Parent == null);
			}
			return null;
		}
		
		public override string Output(int level) {
			StringBuilder str = new StringBuilder();
			str.Append(Utils.IndentedFormat(level, "[CODE BLOCK]\n"));
			foreach( IExecutionTreeNode child in m_Children )
				str.Append(child.Output(level + 1));
			return str.ToString();
		}
	}
}
