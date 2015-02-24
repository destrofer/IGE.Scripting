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

namespace IGE.Scripting {
	[Flags]
	public enum ScriptVariableType : ushort {
		Undefined = 0x0000,

		Bool = 0x0001,

		Char = 0x0002,
		String = 0x0004,

		Byte = 0x0010,
		SByte = 0x0020,
		Short = 0x0040,
		UShort = 0x0080,
		Int = 0x0100,
		UInt = 0x0200,
		Long = 0x0400,
		ULong = 0x0800,

		Float = 0x1000,
		Double = 0x2000,
		Decimal = 0x4000,
		
		Void = 0x8000,
		
		AnyInteger = 0x0FF0,
		AnyFloat = 0x7000,
		AnyNumeric = 0x7FF0,
		AnyValue = 0x7FFF,
	}
	
	public static class ScriptVariableTypeExtension {
		private static Dictionary<ScriptVariableType, int> Priorities = new Dictionary<ScriptVariableType, int>();

		static ScriptVariableTypeExtension() {
			Priorities.Add(ScriptVariableType.Bool, 0);
			Priorities.Add(ScriptVariableType.Byte, 0);
			Priorities.Add(ScriptVariableType.SByte, 0);
			Priorities.Add(ScriptVariableType.Char, 0);
			Priorities.Add(ScriptVariableType.UShort, 0);
			Priorities.Add(ScriptVariableType.Short, 0);
			Priorities.Add(ScriptVariableType.UInt, 0);
			Priorities.Add(ScriptVariableType.Int, 0);
			Priorities.Add(ScriptVariableType.ULong, 0);
			Priorities.Add(ScriptVariableType.Long, 0);
			Priorities.Add(ScriptVariableType.Float, 0);
			Priorities.Add(ScriptVariableType.Double, 0);
			Priorities.Add(ScriptVariableType.Decimal, 0);
			Priorities.Add(ScriptVariableType.String, 0);
		}
		
		public static int ComparePriority(this ScriptVariableType a, ScriptVariableType b) {
			int p1 = Priorities[a], p2 = Priorities[b];
			if( p1 == p2 )
				return 0;
			return (p1 < p2) ? -1 : 1;
		}
		
		public static dynamic GetDefaultValue(this ScriptVariableType type) {
			switch(type) {
				case ScriptVariableType.Bool: return false;
				case ScriptVariableType.Byte: return (byte)0;
				case ScriptVariableType.SByte: return (sbyte)0;
				case ScriptVariableType.Char: return (char)0;
				case ScriptVariableType.UShort: return (ushort)0;
				case ScriptVariableType.Short: return (short)0;
				case ScriptVariableType.UInt: return (uint)0;
				case ScriptVariableType.Int: return (int)0;
				case ScriptVariableType.ULong: return (ulong)0;
				case ScriptVariableType.Long: return (long)0;
				case ScriptVariableType.Float: return (float)0;
				case ScriptVariableType.Double: return (double)0;
				case ScriptVariableType.Decimal: return (decimal)0;
				case ScriptVariableType.String: return null;
				// case ScriptVariableType.Undefined: throw new ScriptCriticalErrorException();
			}
			throw new UserFriendlyException(String.Format("There is no sample value for variable type '{0}'", type), "There was an error in scripting engine");
		}
		
		public static dynamic GetSampleValue(this ScriptVariableType type) {
			switch(type) {
				case ScriptVariableType.Bool: return true;
				case ScriptVariableType.Byte: return (byte)5;
				case ScriptVariableType.SByte: return (sbyte)5;
				case ScriptVariableType.Char: return (char)5;
				case ScriptVariableType.UShort: return (ushort)5;
				case ScriptVariableType.Short: return (short)5;
				case ScriptVariableType.UInt: return (uint)5;
				case ScriptVariableType.Int: return (int)5;
				case ScriptVariableType.ULong: return (ulong)5;
				case ScriptVariableType.Long: return (long)5;
				case ScriptVariableType.Float: return (float)5;
				case ScriptVariableType.Double: return (double)5;
				case ScriptVariableType.Decimal: return (decimal)5;
				case ScriptVariableType.String: return "5";
				// case ScriptVariableType.Undefined: throw new ScriptCriticalErrorException();
			}
			throw new UserFriendlyException(String.Format("There is no sample value for variable type '{0}'", type), "There was an error in scripting engine");
		}
		
		public static Type GetSystemType(this ScriptVariableType type) {
			switch(type) {
				case ScriptVariableType.Bool: return typeof(bool);
				case ScriptVariableType.Byte: return typeof(byte);
				case ScriptVariableType.SByte: return typeof(sbyte);
				case ScriptVariableType.Char: return typeof(char);
				case ScriptVariableType.UShort: return typeof(ushort);
				case ScriptVariableType.Short: return typeof(short);
				case ScriptVariableType.UInt: return typeof(uint);
				case ScriptVariableType.Int: return typeof(int);
				case ScriptVariableType.ULong: return typeof(ulong);
				case ScriptVariableType.Long: return typeof(long);
				case ScriptVariableType.Float: return typeof(float);
				case ScriptVariableType.Double: return typeof(double);
				case ScriptVariableType.Decimal: return typeof(decimal);
				case ScriptVariableType.String: return typeof(string);
			}
			throw new UserFriendlyException(String.Format("There is no sample value for variable type '{0}'", type), "There was an error in scripting engine");
		}
		
		public static ScriptVariableType GetScriptVariableTypeFromType(Type type) {
			if( type.Equals(typeof(bool)) ) return ScriptVariableType.Bool;
			if( type.Equals(typeof(byte)) ) return ScriptVariableType.Byte;
			if( type.Equals(typeof(sbyte)) ) return ScriptVariableType.SByte;
			if( type.Equals(typeof(char)) ) return ScriptVariableType.Char;
			if( type.Equals(typeof(short)) ) return ScriptVariableType.Short;
			if( type.Equals(typeof(ushort)) ) return ScriptVariableType.UShort;
			if( type.Equals(typeof(int)) ) return ScriptVariableType.Int;
			if( type.Equals(typeof(uint)) ) return ScriptVariableType.UInt;
			if( type.Equals(typeof(long)) ) return ScriptVariableType.Long;
			if( type.Equals(typeof(ulong)) ) return ScriptVariableType.ULong;
			if( type.Equals(typeof(float)) ) return ScriptVariableType.Float;
			if( type.Equals(typeof(double)) ) return ScriptVariableType.Double;
			if( type.Equals(typeof(decimal)) ) return ScriptVariableType.Decimal;
			if( type.Equals(typeof(string)) ) return ScriptVariableType.String;
			throw new UserFriendlyException(String.Format("Cannot determine script variable type from type '{0}'", type.FullName), "There was an error in scripting engine");
		}
		
		public static ScriptVariableType GetScriptVariableType(object obj) {
			if( obj is bool ) return ScriptVariableType.Bool;
			if( obj is byte ) return ScriptVariableType.Byte;
			if( obj is sbyte ) return ScriptVariableType.SByte;
			if( obj is char ) return ScriptVariableType.Char;
			if( obj is short ) return ScriptVariableType.Short;
			if( obj is ushort ) return ScriptVariableType.UShort;
			if( obj is int ) return ScriptVariableType.Int;
			if( obj is uint ) return ScriptVariableType.UInt;
			if( obj is long ) return ScriptVariableType.Long;
			if( obj is ulong ) return ScriptVariableType.ULong;
			if( obj is float ) return ScriptVariableType.Float;
			if( obj is double ) return ScriptVariableType.Double;
			if( obj is decimal ) return ScriptVariableType.Decimal;
			if( obj is string ) return ScriptVariableType.String;
			throw new UserFriendlyException(String.Format("Cannot determine script variable type from an object of type '{0}'", obj.GetType().FullName), "There was an error in scripting engine");
		}
	}
}
