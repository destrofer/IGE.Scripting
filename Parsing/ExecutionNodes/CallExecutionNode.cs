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
using System.Reflection;

using IGE.Scripting.InternalExceptions;

namespace IGE.Scripting.ExecutionNodes {
	/// <summary>
	/// </summary>
	public class CallExecutionNode : ExecutionTreeNode {
		public ScriptIdentifierToken Name = null;
		public List<IExecutionTreeNode> Parameters = new List<IExecutionTreeNode>();

		public FunctionNode ScriptFunction = null;
		public bool[] ParamDefault;

		public ScriptEnvironment.FunctionInfo EnvironmentFunction;
		public int FuncParamCount = 0;
		
		public CallExecutionNode(IExecutionTreeNode parent, int line, int character) : base(parent, line, character) {
		}

		public override bool HasResult { get { return true; } }

		public override ScriptVariableType Process(ScriptParser parser, int level) {
			base.Process(parser, level);
			
			// check script if it has a function we can execute
			if( parser.GetFunction(Name.Value, out ScriptFunction) ) {
				if( ScriptFunction.Parameters.Count < Parameters.Count )
					parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.TooManyParameters, this));
				else {
					ParamDefault = new bool[ScriptFunction.Parameters.Count];
					
					for( int i = 0; i < ScriptFunction.Parameters.Count; i++ ) {
						if( Parameters.Count <= i || Parameters[i] == null ) {
							if( ScriptFunction.Parameters[i].HasDefaultValue )
								ParamDefault[i] = true;
							else
								parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.ParameterIsRequired, this, i, Name.Value));
						}
						else {
							ParamDefault[i] = false;
							Parameters[i] = TypeCastExecutionNode.ImplicitCast(parser, level, ScriptFunction.Parameters[i].ParamType, Parameters[i]);
						}
					}
				}
				ResultType = (ScriptFunction.ReturnType == ScriptVariableType.Void) ? ScriptVariableType.Undefined : ScriptFunction.ReturnType;
				
				return ResultType;
			}
			
			// check environment if it has a function we can execute
			if( parser.Script.Environment.GetFunction(Name.Value, out EnvironmentFunction) ) {
				ResultType = EnvironmentFunction.ReturnType;
				
				ParameterInfo[] envFuncParams = EnvironmentFunction.Function.GetParameters();
				FuncParamCount = envFuncParams.Length;
				if( envFuncParams.Length < Parameters.Count )
					parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.TooManyParameters, this));
				else {
					for( int i = 1, j = 0; i < envFuncParams.Length; i++, j++ ) {
						if( Parameters.Count <= j || Parameters[j] == null ) {
							parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.ParameterIsRequired, this, j, Name.Value));
						}
						else {
							if( !envFuncParams[i].ParameterType.Equals(typeof(object)) )
								Parameters[j] = TypeCastExecutionNode.ImplicitCast(parser, level, ScriptVariableTypeExtension.GetScriptVariableTypeFromType(envFuncParams[i].ParameterType), Parameters[j]);
							else
								Parameters[j].Process(parser, level);
						}
					}
				}
				
				return ResultType;
			}
			
			parser.Errors.Add(new ErrorInfo(ErrorLevel.CriticalError, ErrorCode.FunctionNotFound, this, Name.Value));
			throw new ScriptCriticalErrorException();
		}
		
		public override dynamic Execute(ScriptContext context) {
			IExecutionTreeNode node;
			object[] ParamValues;
			dynamic r;

			if( ScriptFunction != null ) {
				ParamValues = new object[ScriptFunction.Parameters.Count];

				for( int i = Parameters.Count - 1; i >= 0; i-- ) {
					if( (node = Parameters[i]) != null )
						ParamValues[i] = node.Execute(context);
				}
				r = ScriptFunction.ExecuteFunction(context, ParamValues, ParamDefault);
				return r;
			}

			ParamValues = new object[FuncParamCount];
			ParamValues[0] = context;
			for( int i = Parameters.Count - 1; i >= 0; i-- )
				if( (node = Parameters[i]) != null )
					ParamValues[i + 1] = node.Execute(context);
			r = EnvironmentFunction.Function.Invoke(EnvironmentFunction.Instance, ParamValues);
			return r;
		}
		
		public override string Output(int level) {
			StringBuilder str = new StringBuilder();
			str.Append(Utils.IndentedFormat(level, "{0}: {1}\n", this.GetType().Name, Name.Value));
			foreach( IExecutionTreeNode par in Parameters ) {
				if( par == null )
					str.Append(Utils.IndentedFormat(level + 1, "[DEFAULT]\n"));
				else
					str.Append(par.Output(level + 1));
			}
			return str.ToString();
		}
	}
}
