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
	public class ScriptLiteralToken : ScriptToken, IExecutionTreeNode {
		public ScriptLiteralTokenType LiteralType;
		public ScriptVariableType ExactType;
		public dynamic CompiledValue;

		public bool HasResult { get { return true; } }
		
		protected IExecutionTreeNode m_Parent = null;
		public IExecutionTreeNode Parent { get { return m_Parent; } set { m_Parent = value; } }
		
		public bool IsLeafNode { get { return true; } }
		
		public ScriptLiteralToken(string val, ScriptLiteralTokenType type, ScriptVariableType exactType, int line, int character) : base(val, line, character) {
			LiteralType = type;
			ExactType = exactType;
			CompiledValue = null;
		}
		
		public virtual ScriptVariableType Process(ScriptParser parser, int level) {
			if( parser.DebugParsing )
				Utils.IndentedOutput(level, "Parse: {0}", GetType().Name);

			if( !ConvertToVariable(ExactType, out CompiledValue) )
				parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.CannotConvertLiteral, this, LiteralType, ExactType));

			if( parser.DebugParsing )
				Utils.IndentedOutput(level, " {0}:{1} => {2}:{3}", Value, LiteralType, CompiledValue, ExactType);

			return ExactType;
		}
		
		public virtual dynamic Execute(ScriptContext context) {
			return CompiledValue;
		}
		
		public virtual string Output(int level) {
			return Utils.IndentedFormat(level, "{0}: {1}\n", this.GetType().Name, this.Value);
		}
		
		public int NumberBase {
			get {
				switch( LiteralType ) {
					case ScriptLiteralTokenType.Binary: return 2;
					case ScriptLiteralTokenType.Octal: return 8;
					case ScriptLiteralTokenType.Hex: return 16;
				}
				return 10;
			}
		}
		
		public bool ConvertToVariable(ScriptVariableType varType, out object result) {
			unchecked {
				result = null;
				try {
					switch( varType ) {
						case ScriptVariableType.Bool:
							if( LiteralType != ScriptLiteralTokenType.Boolean )
								return false;
							result = Value.ToBoolean(false);
							break;
						
						case ScriptVariableType.Byte:
							if( (LiteralType & ScriptLiteralTokenType.Numeric) != ScriptLiteralTokenType.None )
								result = Convert.ToByte(Value, NumberBase);
							else if( LiteralType == ScriptLiteralTokenType.Char )
								result = (byte)Value[0];
							else
								return false;
							break;
	
						case ScriptVariableType.SByte:
							if( (LiteralType & ScriptLiteralTokenType.Numeric) != ScriptLiteralTokenType.None )
								result = Convert.ToSByte(Value, NumberBase);
							else if( LiteralType == ScriptLiteralTokenType.Char )
								result = (sbyte)Value[0];
							else
								return false;
							break;
	
						case ScriptVariableType.Short:
							if( (LiteralType & ScriptLiteralTokenType.Numeric) != ScriptLiteralTokenType.None )
								result = Convert.ToInt16(Value, NumberBase);
							else if( LiteralType == ScriptLiteralTokenType.Char )
								result = (short)Value[0];
							else
								return false;
							break;
	
						case ScriptVariableType.UShort:
							if( (LiteralType & ScriptLiteralTokenType.Numeric) != ScriptLiteralTokenType.None )
								result = Convert.ToUInt16(Value, NumberBase);
							else if( LiteralType == ScriptLiteralTokenType.Char )
								result = (ushort)Value[0];
							else
								return false;
							break;
	
						case ScriptVariableType.Int:
							if( (LiteralType & ScriptLiteralTokenType.Numeric) != ScriptLiteralTokenType.None )
								result = Convert.ToInt32(Value, NumberBase);
							else if( LiteralType == ScriptLiteralTokenType.Char )
								result = (int)Value[0];
							else
								return false;
							break;
	
						case ScriptVariableType.UInt:
							if( (LiteralType & ScriptLiteralTokenType.Numeric) != ScriptLiteralTokenType.None )
								result = Convert.ToUInt32(Value, NumberBase);
							else if( LiteralType == ScriptLiteralTokenType.Char )
								result = (uint)Value[0];
							else
								return false;
							break;
	
						case ScriptVariableType.Long:
							if( (LiteralType & ScriptLiteralTokenType.Numeric) != ScriptLiteralTokenType.None )
								result = Convert.ToInt64(Value, NumberBase);
							else if( LiteralType == ScriptLiteralTokenType.Char )
								result = (long)Value[0];
							else
								return false;
							break;
	
						case ScriptVariableType.ULong:
							if( (LiteralType & ScriptLiteralTokenType.Numeric) != ScriptLiteralTokenType.None )
								result = Convert.ToUInt64(Value, NumberBase);
							else if( LiteralType == ScriptLiteralTokenType.Char )
								result = (ulong)Value[0];
							else
								return false;
							break;

						case ScriptVariableType.Float:
							if( (LiteralType & ScriptLiteralTokenType.Numeric) != ScriptLiteralTokenType.None )
								result = (NumberBase != 10) ? (float)Convert.ToInt32(Value, NumberBase) : Convert.ToSingle(Value);
							else if( LiteralType == ScriptLiteralTokenType.Char )
								result = (float)Value[0];
							else
								return false;
							break;
	
						case ScriptVariableType.Double:
							if( (LiteralType & ScriptLiteralTokenType.Numeric) != ScriptLiteralTokenType.None )
								result = (NumberBase != 10) ? (double)Convert.ToInt64(Value, NumberBase) : Convert.ToDouble(Value);
							else if( LiteralType == ScriptLiteralTokenType.Char )
								result = (double)Value[0];
							else
								return false;
							break;
	
						case ScriptVariableType.Decimal:
							if( (LiteralType & ScriptLiteralTokenType.Numeric) != ScriptLiteralTokenType.None )
								result = (NumberBase != 10) ? (decimal)Convert.ToInt64(Value, NumberBase) : Convert.ToDecimal(Value);
							else if( LiteralType == ScriptLiteralTokenType.Char )
								result = (decimal)Value[0];
							else
								return false;
							break;
	
						case ScriptVariableType.Char:
							if( (LiteralType & ScriptLiteralTokenType.Numeric) != ScriptLiteralTokenType.None )
								result = (char)Convert.ToUInt16(Value, NumberBase);
							else if( LiteralType == ScriptLiteralTokenType.Char )
								result = Value[0];
							else
								return false;
							break;
	
						case ScriptVariableType.String:
							if( LiteralType == ScriptLiteralTokenType.Null )
								result = (string)null;
							else if( LiteralType == ScriptLiteralTokenType.String )
								result = Value;
							else
								return false;
							break;
						
						default:
							return false;
					}
				}
				catch {
					return false;
				}
			}
			return true;
		}
		
		public override string ToString() {
			return String.Format("{0} [{1}]", base.ToString(), LiteralType);
		}
	}
	
	[Flags]
	public enum ScriptLiteralTokenType {
		None = 0x0000,
		
		Null = 0x0001,
		Boolean = 0x0002,
		
		Binary = 0x0010,
		Octal = 0x0020,
		Decimal = 0x0040,
		Hex = 0x0080,
		
		Char = 0x1000,
		String = 0x2000,
		
		Numeric = 0x00F0,
		Strings = 0x3000,
		Any = 0xFFFF,
	}
}