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

namespace IGE.Scripting {
	/// <summary>
	/// </summary>
	public class ScriptEnvironment {
		protected Dictionary<string, FunctionInfo> Functions = new Dictionary<string, FunctionInfo>();
	
		public ScriptEnvironment() {
		}

		public void AddFunction(string name, MethodInfo func) {
			AddFunction(name, func, null);
		}
		
		public void AddFunction(string name, MethodInfo func, object instance) {
			FunctionInfo funcInfo = new FunctionInfo {
				Function = func,
				Instance = instance,
				ReturnType = ScriptVariableType.Undefined
			};
			try {
				if( func.ReturnType != null && !func.ReturnType.Equals(typeof(void)) )
					funcInfo.ReturnType = ScriptVariableTypeExtension.GetScriptVariableTypeFromType(func.ReturnType);
			}
			catch {
				throw new UserFriendlyException("Methods imported as native must return only type acceptable by script (no objects / arrays)", "There was an error in scripting engine");
			}
			ParameterInfo[] param = func.GetParameters();
			if( param.Length < 1 || !(typeof(ScriptContext).IsAssignableFrom(param[0].ParameterType)) )
				throw new UserFriendlyException("Methods imported as native must accept object of ScriptContext as their first parameter", "There was an error in scripting engine");
			
			for( int i = 1; i < param.Length; i++ ) {
				try {
					if( !param[i].ParameterType.Equals(typeof(object)) )
						ScriptVariableTypeExtension.GetScriptVariableTypeFromType(param[i].ParameterType);
				}
				catch {
					throw new UserFriendlyException("All parameters except first one must have type compatible with script (no objects / arrays) in methods imported as native", "There was an error in scripting engine");
				}
			}
			
			Functions.Add(name, funcInfo);
		}
		
		public bool GetFunction(string name, out FunctionInfo func) {
			return Functions.TryGetValue(name, out func);
		}
		
		public object ExecuteFunction(string name, object[] parm) {
			FunctionInfo func = Functions[name];
			return func.Function.Invoke(func.Instance, parm);
		}
		
		public struct FunctionInfo {
			public MethodInfo Function;
			public object Instance;
			public ScriptVariableType ReturnType;
		}
	}
}
