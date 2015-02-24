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

namespace IGE.Scripting {
	/// <summary>
	/// Description of ErrorInfo.
	/// </summary>
	public struct ErrorInfo {
		public ErrorLevel Level;
		public ErrorCode Code;
		public int Character;
		public int Line;
		public object[] Extra;
		
		public LogLevel LogLevel {
			get {
				switch( Level ) {
					case ErrorLevel.CriticalError: return LogLevel.Error;
					case ErrorLevel.Error: return LogLevel.Error;
					case ErrorLevel.Warning: return LogLevel.Warning;
					case ErrorLevel.Notice: return LogLevel.Debug;
					default: return LogLevel.Debug;
				}
			}
		}
		
		public string GetExtra(int index) {
			if( Extra == null || index < 0 || index >= Extra.Length )
				return String.Format("[error param {0} is not defined]", index);
			return Extra[index].ToString();
		}
		
		public string Message {
			get {
				switch( Code ) {
					case ErrorCode.UnreachableCode: return String.Format("Unreachable code detected on line {0} at character {1}", Line, Character);
					case ErrorCode.NotAllCodePathsReturnAValue: return String.Format("Not all code paths return a value in function on line {0} at character {1}", Line, Character);
					case ErrorCode.UndefinedVariable: return String.Format("Undefned variable '{2}' on line {0} at character {1}", Line, Character, GetExtra(0));
					case ErrorCode.VariableAlreadyExists: return String.Format("Variable '{2}' already exists in current scope on line {0} at character {1}", Line, Character, GetExtra(0));
					case ErrorCode.NoExplicitConversion: return String.Format("Cannot explicitly convert from '{2}' to '{3}' on line {0} at character {1}", Line, Character, GetExtra(0), GetExtra(1));
					case ErrorCode.NoImplicitConversion: return String.Format("Cannot implicitly convert from '{2}' to '{3}' on line {0} at character {1}", Line, Character, GetExtra(0), GetExtra(1));
					case ErrorCode.CannotConvertLiteral: return String.Format("Cannot implicitly convert '{2}' literal to '{3}' on line {0} at character {1}", Line, Character, GetExtra(0), GetExtra(1));
					case ErrorCode.OperatorCannotBeApplied1: return String.Format("Operator cannot be applied to '{2}' on line {0} at character {1}", Line, Character, GetExtra(0));
					case ErrorCode.OperatorCannotBeApplied2: return String.Format("Operator cannot be applied to '{2}' and '{3}' on line {0} at character {1}", Line, Character, GetExtra(0), GetExtra(1));
					case ErrorCode.ParameterIsRequired: return String.Format("Parameter #{2} is required to call function '{3}' on line {0} at character {1}", Line, Character, GetExtra(0), GetExtra(1));
					case ErrorCode.FunctionDoesNotReturnAValue: return String.Format("Function '{2}' does not return a value on line {0} at character {1}", Line, Character, GetExtra(0));
					case ErrorCode.FunctionNotFound: return String.Format("Function '{2}' not found either in script itself nor in it's environment on line {0} at character {1}", Line, Character, GetExtra(0));
				}
				return String.Format("Unknown error '{0}' on line {1} at character {2}", Code, Line, Character);
			}
		}
		
		public ErrorInfo(ErrorLevel level, ErrorCode code, IExecutionTreeNode node) : this(level, code, node, null) {
		}
		
		public ErrorInfo(ErrorLevel level, ErrorCode code, IExecutionTreeNode node, params object[] extra) {
			Level = level;
			Code = code;
			Character = node.Character;
			Line = node.Line;
			Extra = extra;
		}
	}
}
