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

namespace IGE.Scripting.ExecutionNodes {
	/// <summary>
	/// </summary>
	public class VarDefinitionExecutionNode : ExecutionTreeNode {
		public ScriptVariableType VarType = ScriptVariableType.Undefined;
		public List<Definition> Definitions = new List<Definition>();
		
		public VarDefinitionExecutionNode(IExecutionTreeNode parent, int line, int character) : base(parent, line, character) {
		}

		public override bool HasResult { get { return false; } }

		public override ScriptVariableType Process(ScriptParser parser, int level) {
			base.Process(parser, level);

			foreach( Definition def in Definitions ) {
				if( !parser.AddVariable(new ScriptVariableDefinition(VarType, def.Name)) )
					parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.VariableAlreadyExists, def.Name, def.Name.Value));
				if( def.DefaultValue != null )
					def.DefaultValue = TypeCastExecutionNode.ImplicitCast(parser, level + 1, VarType, def.DefaultValue);
				else
					def.UnassignedDefaultValue = VarType.GetDefaultValue();
			}
			return ScriptVariableType.Undefined;
		}
		
		public override dynamic Execute(ScriptContext context) {
			foreach( Definition def in Definitions ) {
				dynamic v = (def.DefaultValue == null) ? def.UnassignedDefaultValue : def.DefaultValue.Execute(context);
				context.AddVariable(def.Name.Value, v);
			}
			return null;
		}
		
		public override string Output (int level) {
			StringBuilder str = new StringBuilder();
			str.Append(Utils.IndentedFormat(level, "{0}: {1}\n", this.GetType().Name, VarType));
			foreach( Definition def in Definitions ) {
				str.Append(Utils.IndentedFormat(level + 1, "{0}\n", def.Name.Value));
				if( def.DefaultValue == null )
					str.Append(Utils.IndentedFormat(level + 2, "[NO DEFAULT VALUE]\n"));
				else
					str.Append(def.DefaultValue.Output(level + 2));
			}
			return str.ToString();
		}
		
		public class Definition {
			public ScriptIdentifierToken Name;
			public IExecutionTreeNode DefaultValue;
			public dynamic UnassignedDefaultValue;
			
			public Definition(ScriptIdentifierToken name, IExecutionTreeNode defaultValue) {
				Name = name;
				DefaultValue = defaultValue;
				UnassignedDefaultValue = null;
			}
		}
	}
}
