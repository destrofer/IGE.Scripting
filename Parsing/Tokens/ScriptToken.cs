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
using System.Text.RegularExpressions;

namespace IGE.Scripting {
	public abstract class ScriptToken {
		protected int m_Line;
		protected int m_Character;
		public string Value;
		
		public int Line { get { return m_Line; } set { m_Line = value; } }
		public int Character { get { return m_Character; } set { m_Character = value; } }
		
		public ScriptToken(string val, int line, int character) {
			Value = val;
			m_Line = line;
			m_Character = character;
		}
		
		public virtual IExecutionTreeNode Compile(Script script) {
			throw new InvalidOperationException("Cannot compile an operator or keyword token into execution tree.");
		}
		
		public virtual string ToVerboseString() {
			return String.Format("{0}: {1} ({2}:{3})", GetType().Name, Value, Line, Character);
		}
		
		public override string ToString() {
			return String.Format("{0}: {1}", GetType().Name, Value);
		}
	}
}