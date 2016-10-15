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
using System.Text;
using System.Text.RegularExpressions;

namespace IGE.Scripting.VM {
	public class AssemblerCompiler {
		protected int InterruptCount;
		
		public AssemblerCompiler(VirtualMachine targetMachine)
			: this(targetMachine.InterruptCount)
		{
		}

		public AssemblerCompiler(int targetInterruptCount) {
			InterruptCount = targetInterruptCount;
		}
		
		public byte[] Compile(string code, out List<Error> errors) {
			CompilationContext context = new CompilationContext();
			byte[] compiledCode = null;

			context.AddCode(code);

			if( context.Errors.Count == 0 )
				compiledCode = context.Compile();
			
			errors = context.Errors;
			return compiledCode;
		}
		
		public class CompilationContext {
			public int Line = 1;
			public int Character = 0;
			public int State = 0;
			public List<Error> Errors = new List<Error>();
			public Dictionary<string, int> Labels = new Dictionary<string, int>();
			public List<CodeBlock> CompiledCode = new List<CodeBlock>();
			public ByteQueue CurrentBlock = new ByteQueue();
			public StringBuilder Token = new StringBuilder();
			public string CurrentInstruction = null;
			public string CurrentOperand1 = null;
			public string CurrentOperand2 = null;

			public void AddCode(string code) {
				char c;
				
				for( int pos = 0, size = code.Length; pos <= size; pos++ ) {
					// new line in the end is needed just to make sure that the last line is processed as code
					c = (pos < size) ? code[pos] : '\n';
					Character++;
					
					switch(State) {
							case -1: { // skip until the end of line
								if( c == '\n' )
									NextLine();
								break;
							}
							
							case 0: { // new line and we are expecting either label or an instruction 
								if( c == ' ' || c == '\t' || c == '\r' ) {
									if( Token.Length > 0 ) {
										CurrentInstruction = Token.ToString();
										Token.Clear();
										State = 2;
									}
									break;
								}
								if( c == ':' ) {
									AddLabel(Token.ToString());
									Token.Clear();
									break;
								}
								
								if( (c == ';' || c == '\n') && Token.Length > 0 ) {
									CurrentInstruction = Token.ToString();
									AddInstruction();
								}
								if( c == ';' ) {
									State = -1;
									break;
								}
								if( c == '\n' ) {
									NextLine();
									break;
								}
								
								if( (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ) {
									// still unknown ... either label or instruction 
									Token.Append(c);
									break;
								}
								
								if( c == '_' ) {
									// only label may contain symbol "_".
									Token.Append(c);
									State = 1;
									break;
								}
								
								Errors.Add(new Error(String.Format("Unexpected symbol on line {0} at character {1}", Line, Character), Line, Character));
								State = -1; // skip until the end of line
								
								break;
							}
							
							case 1: { // expecting the token to end with ":".
								if( c == '_' || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ) {
									// label name continues 
									Token.Append(c);
									break;
								}
								
								if( c == ':' ) {
									AddLabel(Token.ToString());
									Token.Clear();
									State = 0; // there still may be other labels or an instruction after this one
									break;
								}
								
								Errors.Add(new Error(String.Format("Unexpected symbol on line {0} at character {1}", Line, Character), Line, Character));
								State = -1; // skip until the end of line
								
								break;
							}
							
							case 2: { // ok, so previously we had an instruction. now we are expecting operands
								if( c == ';' || c == '\n' ) {
									if( Token.Length > 0 ) {
										if( CurrentOperand1 == null ) { 
											CurrentOperand1 = Token.ToString().Trim();
											if( CurrentOperand1.Length == 0 ) // if the text after the instruction is empty then there is no operand
												CurrentOperand1 = null;
										}
										else // we are currently reading the second operand
											CurrentOperand2 = Token.ToString().Trim();
									}
									
									AddInstruction();
									
									if( c == '\n' )
										NextLine();
									else
										State = -1;
									break;
								}
								if( c == ',' ) {
									if( CurrentOperand1 != null ) {
										Errors.Add(new Error(String.Format("Too many operands on line {0} at character {1}", Line, Character), Line, Character));
										State = -1;
										break;
									}
									CurrentOperand1 = Token.ToString().Trim();
									Token.Clear();
									break;
								}
								
								Token.Append(c);
								break;
							}
					}
				}
				
				PushCurrentBlock();
				GameDebugger.Log(LogLevel.Debug, "Final number of blocks: {0}", CompiledCode.Count);
			}
			
			protected void NextLine() {
				Token.Clear();
				Line++;
				Character = 0;
				State = 0;
				CurrentInstruction = null;
				CurrentOperand1 = null;
				CurrentOperand2 = null;
			}
			
			protected void AddLabel(string name) {
				string lcLabel = name.ToLower();

				PushCurrentBlock();
				
				if( Labels.ContainsKey(lcLabel) )
					Errors.Add(new Error(String.Format("Duplicate label '{0}' on line {1} at character {2}.", name, Line, Character), Line, Character));
				else {
					GameDebugger.Log(LogLevel.Debug, "Adding label {0} at block {1}", name, CompiledCode.Count);
					Labels.Add(lcLabel, CompiledCode.Count); // the label will point to the next code block
				}
			}
			
			protected void AddInstruction() {
				if( CurrentOperand1 != null && CurrentOperand1.Length == 0 ) {
					Errors.Add(new Error(String.Format("First operand is missing on line {0} at character {1}", Line, Character), Line, Character));
					return;
				}
				if( CurrentOperand2 != null && CurrentOperand2.Length == 0 ) {
					Errors.Add(new Error(String.Format("Second operand is missing on line {0} at character {1}", Line, Character), Line, Character));
					return;
				}

				if( CurrentInstruction.Equals("JL") || CurrentInstruction.Equals("JMP") ) {
					if( CurrentOperand1 == null ) {
						Errors.Add(new Error(String.Format("Jump instruction needs a label to jump to on line {0} at character {1}", Line, Character), Line, Character));
						return;
					}
						
					PushCurrentBlock();
					GameDebugger.Log(LogLevel.Debug, "Adding jump instruction {0} {1}", CurrentInstruction, CurrentOperand1 ?? "NULL");
					GameDebugger.Log(LogLevel.Debug, "Pushing JUMP block {0}", CompiledCode.Count);
					CompiledCode.Add(new JumpCodeBlock(0, CurrentOperand1));
				}
				else {
					if( CurrentOperand2 != null )
						GameDebugger.Log(LogLevel.Debug, "Adding instruction {0} {1}, {2}", CurrentInstruction, CurrentOperand1, CurrentOperand2);
					else if( CurrentOperand1 != null )
						GameDebugger.Log(LogLevel.Debug, "Adding instruction {0} {1}", CurrentInstruction, CurrentOperand1);
					else
						GameDebugger.Log(LogLevel.Debug, "Adding instruction {0}", CurrentInstruction);
					
					CurrentBlock.Enqueue(0);
				}
			}
			
			protected void PushCurrentBlock() {
				if( CurrentBlock.Length > 0 ) {
					GameDebugger.Log(LogLevel.Debug, "Pushing block {0}", CompiledCode.Count);
					CompiledCode.Add(new CodeBlock(CurrentBlock.Dequeue(CurrentBlock.Length)));
				}
			}
			
			public byte[] Compile() {
				ByteQueue code = new ByteQueue();
				int i, il;
				
				// process jump, loop and call instruction blocks and expand them to far jumps if near is not enough is not enough  
				foreach( CodeBlock block in CompiledCode ) {
					if( block is JumpCodeBlock ) {
						block.CompiledData = new byte[1] { ((JumpCodeBlock)block).Instruction };
					}
				}
				
				// finaly collect compiled code into one array
				foreach( CodeBlock block in CompiledCode ) {
					code.Enqueue(block.CompiledData);
				}
				
				return code.Dequeue(code.Length);
			}
		}
		                         
		public class CodeBlock {
			public int Offset = 0;
			public int Length = 0;
			public byte[] CompiledData = null;
			
			public CodeBlock(byte[] compiledData) {
				CompiledData = compiledData;
				if( compiledData != null )
					Length = compiledData.Length;
			}
		}
		
		public class JumpCodeBlock : CodeBlock {
			public byte Instruction;
			public string TargetLabel;
			public int JumpOffset = 0;
			
			public JumpCodeBlock(byte instruction, string targetLabel)
				: base(null)
			{
				Instruction = instruction;
				TargetLabel = targetLabel;
				
				// Initially jump instruction uses only 8 bits in offset operand,
				// but compiler will increase the size to 32 bits if jump distance
				// is/gets greater than can be supported by 8 bits.
				Length = 2;
			}
		}
		
		public struct Error {
			public string Text;
			public int Line;
			public int Character;
			
			public Error(string text, int line, int character) {
				Text = text;
				Line = line;
				Character = character;
			}
		}
	}
}
