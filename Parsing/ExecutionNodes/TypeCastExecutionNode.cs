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

namespace IGE.Scripting.ExecutionNodes {
	/// <summary>
	/// </summary>
	public class TypeCastExecutionNode : ExecutionTreeNode {
		public ScriptVariableType CastToType = ScriptVariableType.Undefined;
		public ScriptVariableType CastFromType = ScriptVariableType.Undefined;
		public Type CastToSystemType = null;
		public IExecutionTreeNode Value = null;
		public bool IsImplicit = false;
		
		public TypeCastExecutionNode(IExecutionTreeNode parent, int line, int character) : base(parent, line, character) {
		}

		public override bool HasResult { get { return true; } }

		public override ScriptVariableType Process(ScriptParser parser, int level) {
			base.Process(parser, level);

			CastFromType = Value.Process(parser, level + 1);
			CastToSystemType = CastToType.GetSystemType();

			dynamic a = CastFromType.GetSampleValue();

			try {
				a = Convert.ChangeType(a, CastToSystemType);
			}
			catch {
				parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.NoExplicitConversion, this, CastFromType, CastToType));
			}
			
			if( parser.DebugParsing )
				Utils.IndentedOutput(level, "Explicit cast from '{0}' to '{1}' ({2})", CastFromType, CastToType, CastToSystemType);
			
			return CastToType;
		}

		public static IExecutionTreeNode ExplicitCast(IExecutionTreeNode node, ScriptVariableType fromType, ScriptVariableType toType) {
			TypeCastExecutionNode typeCast = new TypeCastExecutionNode(node.Parent, node.Line, node.Character);
			node.Parent = typeCast;
			typeCast.CastFromType = fromType;
			typeCast.CastToType = toType;
			typeCast.Value = node;
			return typeCast;
		}
		
		public static IExecutionTreeNode ImplicitCast(ScriptParser parser, int level, ScriptVariableType requiredType, IExecutionTreeNode node) {
			ScriptVariableType acceptableTypes = ScriptVariableType.Undefined;
			// implicit cast is allowed only in loseless cases (except floating point types)
			switch( requiredType ) {
				case ScriptVariableType.Short: acceptableTypes = ScriptVariableType.Short | ScriptVariableType.Byte | ScriptVariableType.SByte; break;
				case ScriptVariableType.UShort: acceptableTypes = ScriptVariableType.UShort | ScriptVariableType.Byte; break;
				case ScriptVariableType.Int: acceptableTypes = ScriptVariableType.Int | ScriptVariableType.UShort | ScriptVariableType.Short | ScriptVariableType.Byte | ScriptVariableType.SByte; break;
				case ScriptVariableType.UInt: acceptableTypes = ScriptVariableType.UInt | ScriptVariableType.UShort | ScriptVariableType.Byte; break;
				case ScriptVariableType.Long: acceptableTypes = ScriptVariableType.Long | ScriptVariableType.UInt | ScriptVariableType.Int | ScriptVariableType.UShort | ScriptVariableType.Short | ScriptVariableType.Byte | ScriptVariableType.SByte; break;
				case ScriptVariableType.ULong: acceptableTypes = ScriptVariableType.ULong | ScriptVariableType.UInt | ScriptVariableType.UShort | ScriptVariableType.Byte; break;
				case ScriptVariableType.Float: acceptableTypes = ScriptVariableType.Float | ScriptVariableType.UInt | ScriptVariableType.Int | ScriptVariableType.UShort | ScriptVariableType.Short | ScriptVariableType.Byte | ScriptVariableType.SByte; break;
				case ScriptVariableType.Double: acceptableTypes = ScriptVariableType.Double | ScriptVariableType.Float | ScriptVariableType.ULong | ScriptVariableType.Long | ScriptVariableType.UInt | ScriptVariableType.Int | ScriptVariableType.UShort | ScriptVariableType.Short | ScriptVariableType.Byte | ScriptVariableType.SByte; break;
				case ScriptVariableType.Decimal: acceptableTypes = ScriptVariableType.Decimal | ScriptVariableType.Double | ScriptVariableType.Float | ScriptVariableType.ULong | ScriptVariableType.Long | ScriptVariableType.UInt | ScriptVariableType.Int | ScriptVariableType.UShort | ScriptVariableType.Short | ScriptVariableType.Byte | ScriptVariableType.SByte; break;
			}
			acceptableTypes = acceptableTypes | requiredType;
			ScriptVariableType type = node.Process(parser, level);
			if( type == requiredType )
				return node;
			
			if( (type & acceptableTypes) == ScriptVariableType.Undefined ) {
				parser.Errors.Add(new ErrorInfo(ErrorLevel.Error, ErrorCode.NoImplicitConversion, node, type, requiredType));
				return node; // assume when the error is fixed no implicit cast will be required
			}
			
			TypeCastExecutionNode typeCast = new TypeCastExecutionNode(node.Parent, node.Line, node.Character);
			node.Parent = typeCast;
			typeCast.CastFromType = type;
			typeCast.CastToType = requiredType;
			typeCast.Value = node;
			typeCast.IsImplicit = true;
			
			typeCast.CastToSystemType = typeCast.CastToType.GetSystemType();

			if( parser.DebugParsing )
				Utils.IndentedOutput(level, "Inserting implicit cast from '{0}' to '{1}' ({2})", typeCast.CastFromType, typeCast.CastToType, typeCast.CastToSystemType);
			
			return typeCast;
		}

		public static ScriptVariableType GetAcceptableTypes(ScriptVariableType requiredResultType) {
			// Explicit cast exists from any to any type
			return ScriptVariableType.AnyValue;
			/*
			switch( requiredResultType ) {
				case ScriptVariableType.Bool: return ScriptVariableType.Bool;
				case ScriptVariableType.Byte: return ScriptVariableType.Byte | ScriptVariableType.SByte | ScriptVariableType.Short | ScriptVariableType.UShort | ScriptVariableType.Int | ScriptVariableType.UInt | ScriptVariableType.Long | ScriptVariableType.ULong | ScriptVariableType.Float | ScriptVariableType.Double | ScriptVariableType.Decimal | ScriptVariableType.Char | ScriptVariableType.String;
				case ScriptVariableType.SByte: return ScriptVariableType.Byte | ScriptVariableType.SByte | ScriptVariableType.Short | ScriptVariableType.UShort | ScriptVariableType.Int | ScriptVariableType.UInt | ScriptVariableType.Long | ScriptVariableType.ULong | ScriptVariableType.Float | ScriptVariableType.Double | ScriptVariableType.Decimal | ScriptVariableType.Char | ScriptVariableType.String;
				case ScriptVariableType.Short: return ScriptVariableType.Byte | ScriptVariableType.SByte | ScriptVariableType.Short | ScriptVariableType.UShort | ScriptVariableType.Int | ScriptVariableType.UInt | ScriptVariableType.Long | ScriptVariableType.ULong | ScriptVariableType.Float | ScriptVariableType.Double | ScriptVariableType.Decimal | ScriptVariableType.Char | ScriptVariableType.String;
				case ScriptVariableType.UShort: return ScriptVariableType.Byte | ScriptVariableType.SByte | ScriptVariableType.Short | ScriptVariableType.UShort | ScriptVariableType.Int | ScriptVariableType.UInt | ScriptVariableType.Long | ScriptVariableType.ULong | ScriptVariableType.Float | ScriptVariableType.Double | ScriptVariableType.Decimal | ScriptVariableType.Char | ScriptVariableType.String;
				case ScriptVariableType.Int: return ScriptVariableType.Byte | ScriptVariableType.SByte | ScriptVariableType.Short | ScriptVariableType.UShort | ScriptVariableType.Int | ScriptVariableType.UInt | ScriptVariableType.Long | ScriptVariableType.ULong | ScriptVariableType.Float | ScriptVariableType.Double | ScriptVariableType.Decimal | ScriptVariableType.Char | ScriptVariableType.String;
				case ScriptVariableType.UInt: return ScriptVariableType.Byte | ScriptVariableType.SByte | ScriptVariableType.Short | ScriptVariableType.UShort | ScriptVariableType.Int | ScriptVariableType.UInt | ScriptVariableType.Long | ScriptVariableType.ULong | ScriptVariableType.Float | ScriptVariableType.Double | ScriptVariableType.Decimal | ScriptVariableType.Char | ScriptVariableType.String;
				case ScriptVariableType.Long: return ScriptVariableType.Byte | ScriptVariableType.SByte | ScriptVariableType.Short | ScriptVariableType.UShort | ScriptVariableType.Int | ScriptVariableType.UInt | ScriptVariableType.Long | ScriptVariableType.ULong | ScriptVariableType.Float | ScriptVariableType.Double | ScriptVariableType.Decimal | ScriptVariableType.Char | ScriptVariableType.String;
				case ScriptVariableType.ULong: return ScriptVariableType.Byte | ScriptVariableType.SByte | ScriptVariableType.Short | ScriptVariableType.UShort | ScriptVariableType.Int | ScriptVariableType.UInt | ScriptVariableType.Long | ScriptVariableType.ULong | ScriptVariableType.Float | ScriptVariableType.Double | ScriptVariableType.Decimal | ScriptVariableType.Char | ScriptVariableType.String;
				case ScriptVariableType.Float: return ScriptVariableType.Byte | ScriptVariableType.SByte | ScriptVariableType.Short | ScriptVariableType.UShort | ScriptVariableType.Int | ScriptVariableType.UInt | ScriptVariableType.Long | ScriptVariableType.ULong | ScriptVariableType.Float | ScriptVariableType.Double | ScriptVariableType.Decimal | ScriptVariableType.Char | ScriptVariableType.String;
				case ScriptVariableType.Double: return ScriptVariableType.Byte | ScriptVariableType.SByte | ScriptVariableType.Short | ScriptVariableType.UShort | ScriptVariableType.Int | ScriptVariableType.UInt | ScriptVariableType.Long | ScriptVariableType.ULong | ScriptVariableType.Float | ScriptVariableType.Double | ScriptVariableType.Decimal | ScriptVariableType.Char | ScriptVariableType.String;
				case ScriptVariableType.Decimal: return ScriptVariableType.Byte | ScriptVariableType.SByte | ScriptVariableType.Short | ScriptVariableType.UShort | ScriptVariableType.Int | ScriptVariableType.UInt | ScriptVariableType.Long | ScriptVariableType.ULong | ScriptVariableType.Float | ScriptVariableType.Double | ScriptVariableType.Decimal | ScriptVariableType.Char | ScriptVariableType.String;
				case ScriptVariableType.Char: return ScriptVariableType.Byte | ScriptVariableType.SByte | ScriptVariableType.Short | ScriptVariableType.UShort | ScriptVariableType.Int | ScriptVariableType.UInt | ScriptVariableType.Long | ScriptVariableType.ULong | ScriptVariableType.Float | ScriptVariableType.Double | ScriptVariableType.Decimal | ScriptVariableType.Char | ScriptVariableType.String;
				case ScriptVariableType.String: return ScriptVariableType.Bool | ScriptVariableType.Byte | ScriptVariableType.SByte | ScriptVariableType.Short | ScriptVariableType.UShort | ScriptVariableType.Int | ScriptVariableType.UInt | ScriptVariableType.Long | ScriptVariableType.ULong | ScriptVariableType.Float | ScriptVariableType.Double | ScriptVariableType.Decimal | ScriptVariableType.Char | ScriptVariableType.String;
			}
			throw new UserFriendlyException(String.Format("{0} cannot decide on acceptable types for {1}", this.GetType().Name, requiredResultType), "An error in scripting engine was encountered");
			*/
		}

		
		public override dynamic Execute(ScriptContext context) {
			dynamic a = Value.Execute(context);
			dynamic r = Convert.ChangeType(a, CastToSystemType);
			return r;
		}
		
		public override string Output (int level) {
			return Utils.IndentedFormat(level, "{0}: {1}\n{2}", this.GetType().Name, CastToType, Value.Output(level + 1));
		}
	}
}
