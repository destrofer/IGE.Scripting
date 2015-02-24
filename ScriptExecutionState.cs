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
	/// <summary>
	/// </summary>
	public class ScriptExecutionState {
		protected Dictionary<string, ScriptVariable> m_Variables = new Dictionary<string, ScriptVariable>();

		protected ScriptContext m_Context;
		public ScriptContext Context { get { return m_Context; } }
		
		protected object m_ExitValue = null;
		public object ExitValue { get { return m_ExitValue; } set { m_ExitValue = value; } }
		
		public ScriptExecutionState(ScriptContext context) {
			m_Context = context;
		}
	}
}
