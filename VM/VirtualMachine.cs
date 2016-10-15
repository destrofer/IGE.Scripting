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
using System.Linq;
using System.Collections.Generic;

namespace IGE.Scripting.VM {
	public class VirtualMachine {
		public VirtualDevice[] Devices = null;
		public VirtualDevice[] Ports = null;
		public byte[] Memory = null;
		
		public ulong RAX = 0;
		public ulong RBX = 0;
		public ulong RCX = 0;
		public ulong RDX = 0;

		public uint BP = 0;
		public uint DP = 0;
		public uint SP = 0;
		public uint IP = 0;

		public ushort FLAGS = 0;

		public int InterruptCount = 32;
		
		public VirtualMachine(int interruptCount, int memoryLimit) {
			InterruptCount = interruptCount;
			Memory = new byte[memoryLimit];
			
			IP = BP = DP = (uint)(interruptCount * 4);
			SP = (uint)(memoryLimit - 1);
			if( SP <= DP )
				throw new UserFriendlyException(String.Format("Virtual machine memory size is too small ({0} bytes) to support {1} interrupts.", memoryLimit, interruptCount), "There was an error in virtual machine creation code");
		}

		public int WriteRom(byte[] rom) {
			return WriteRom(0, rom, rom.Length);
		}

		public int WriteRom(int offset, byte[] rom) {
			return WriteRom(offset, rom, rom.Length);
		}
		
		public int WriteRom(int offset, byte[] rom, int length) {
			length = Math.Min(length, Memory.Length - offset);
			Array.Copy(rom, offset, Memory, 0, length);
			return length;
		}
	}
}
