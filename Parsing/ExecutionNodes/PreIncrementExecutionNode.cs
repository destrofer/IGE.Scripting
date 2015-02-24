﻿/*
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

namespace IGE.Scripting.ExecutionNodes {
	/// <summary>
	/// </summary>
	public class PreIncrementExecutionNode : Math1ExecutionNode {
		public override bool AffectsVariable { get { return true; } }
		
		public PreIncrementExecutionNode(IExecutionTreeNode parent, int line, int character) : base(parent, line, character) {
		}
		
		public override dynamic ApplyOperator(ref dynamic a) {
			return ++a;
		}

		public override string Output(int level) {
			return Utils.IndentedFormat(level, "{0}: {1}\n", this.GetType().Name, ((ScriptIdentifierToken)Value).Value);
		}
	}
}
