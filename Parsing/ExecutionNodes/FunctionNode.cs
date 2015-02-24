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
	public class FunctionNode : ExecutionTreeNode {
		public ScriptVariableType ReturnType = ScriptVariableType.Undefined;
		public ScriptIdentifierToken Name = null;
		public List<FunctionParameter> Parameters = new List<FunctionParameter>();
		public IExecutionTreeNode Body = null;
		
		public FunctionNode(IExecutionTreeNode rootNode, int line, int character) : base(rootNode, line, character) {
		}

		public override bool HasResult { get { return false; } }
		
		public override ScriptVariableType Process(ScriptParser parser, int level) {
			base.Process(parser, level);
			parser.IncrementScopeLevel(true);

			foreach( FunctionParameter param in Parameters ) {
				if( param.HasDefaultValue ) {
					if( param.ParamType == ScriptVariableType.String ) {
						if( !param.DefaultValue.ConvertToVariable(ScriptVariableType.String, out param.CompiledDefaultValue) )
							parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.BadDefaultForStringParameter, param.DefaultValue));
					}
					else if( param.ParamType == ScriptVariableType.Char ) {
						if( !param.DefaultValue.ConvertToVariable(ScriptVariableType.Char, out param.CompiledDefaultValue) )
							parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.BadDefaultForCharParameter, param.DefaultValue));
					}
					else if( param.ParamType == ScriptVariableType.Bool ) {
						if( !param.DefaultValue.ConvertToVariable(ScriptVariableType.Bool, out param.CompiledDefaultValue) )
							parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.BadDefaultForBooleanParameter, param.DefaultValue));
					}
					else {
						if( !param.DefaultValue.ConvertToVariable(param.ParamType, out param.CompiledDefaultValue) )
							parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.BadDefaultForNumericParameter, param.DefaultValue));
					}
				}

				parser.AddVariable(new ScriptVariableDefinition(param.ParamType, param.Name));
			}
			
			parser.RequiredReturnType = ReturnType;
			Body.Process(parser, 0);
			parser.RequiredReturnType = ScriptVariableType.Undefined;
			
			parser.DecrementScopeLevel(true);
			
			return ScriptVariableType.Undefined;
		}

		public virtual object ExecuteFunction(ScriptContext context, object[] paramValues, bool[] useDefault) {
			context.IncrementScopeLevel(true);
			try {
				FunctionParameter parm;
				
				for( int i = Parameters.Count - 1; i >= 0 ; i-- ) {
					parm = Parameters[i];
					context.AddVariable(parm.Name.Value, useDefault[i] ? parm.CompiledDefaultValue : paramValues[i]);
				}
				Body.Execute(context);
			}
			catch(ScriptExecutionReturnException ex) {
				return ex.ReturnValue;
			}
			finally {
				context.DecrementScopeLevel(true);
			}
			return null;
		}
		
		public override dynamic Execute(ScriptContext context) {
			throw new UserFriendlyException("Use ExecuteFunction() instead of Execute()", "There was an error in scripting engine");
		}
		
		public override string Output (int level) {
			return Utils.IndentedFormat(level, "{0}: {1}\n{2}", this.GetType().Name, Name.Value, Body.Output(level + 1));
		}
		
		public class FunctionParameter {
			public ScriptVariableType ParamType = ScriptVariableType.Undefined;
			public ScriptIdentifierToken Name = null;
			public ScriptLiteralToken DefaultValue = null;
			public object CompiledDefaultValue = null;
			public bool HasDefaultValue = false;
			
			public FunctionParameter() {
			}
		}
	}
}
