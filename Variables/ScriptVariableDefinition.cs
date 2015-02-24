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

namespace IGE.Scripting
{
	/// <summary>
	/// Description of ScriptVariableDefinition.
	/// </summary>
	public struct ScriptVariableDefinition : IEquatable<ScriptVariableDefinition> {
		public ScriptVariableType VarType;
		public ScriptIdentifierToken Name;
		
		public ScriptVariableDefinition(ScriptVariableType varType, ScriptIdentifierToken name) {
			VarType = varType;
			Name = name;
		}
		
		#region Equals and GetHashCode implementation
		public override bool Equals(object obj) {
			if (obj is ScriptVariableDefinition)
				return Equals((ScriptVariableDefinition)obj); 
			else
				return false;
		}
		
		public bool Equals(ScriptVariableDefinition other) {
			return this.Name.Value.Equals(other.Name.Value) && this.VarType == other.VarType;
		}
		
		public override int GetHashCode() {
			return Name.Value.GetHashCode();
		}
		
		public static bool operator ==(ScriptVariableDefinition left, ScriptVariableDefinition right) {
			return left.Equals(right);
		}
		
		public static bool operator !=(ScriptVariableDefinition left, ScriptVariableDefinition right) {
			return !left.Equals(right);
		}
		#endregion
	}
}
