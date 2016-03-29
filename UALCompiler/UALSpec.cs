using System;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
namespace UALCompiler
{
	public static class Extensions {
		/// <summary>
		/// Encodes a string in UTF-8 null-terminated format.
		/// </summary>
		/// <param name="writer">The writer to use</param>
		/// <param name="value">The string to encode</param>
		public static void WriteString(this BinaryWriter writer, string value) {
			writer.Write (Encoding.UTF8.GetBytes (value));
			writer.Write ((byte)0);
		}
	}
	public abstract class UALSerializable {
		/// <summary>
		/// Serialize this instance to a UAL bytestream.
		/// </summary>
		public abstract byte[] Serialize ();
	}
	public class UALField:UALSerializable {
		FieldDefinition info;
		public UALField(FieldDefinition info) {
			this.info = info;
		}
		public override byte[] Serialize ()
		{
			MemoryStream mstream = new MemoryStream ();
			return mstream.ToArray ();
		}
	}

	public class UALMethod:UALSerializable {
		delegate void code_emit (Instruction il,byte opcode);
		//Table of all imports
		public static Dictionary<string,int> MethodTable = new Dictionary<string, int>();


		static int currentMethodPointer = 0; 
		static int GetMethodPointer(string signature) {
			if (MethodTable.ContainsKey (signature)) {
				return MethodTable [signature];
			}
			currentMethodPointer++;
			MethodTable [signature] = currentMethodPointer-1;
			return currentMethodPointer - 1;
		}


		MethodDefinition info;

		public UALMethod(MethodDefinition info) {
			this.info = info;
		}
		public override byte[] Serialize ()
		{
			//OPCODE list:


			/**
			 * 0 -- Load argument
			 * 1 -- Call function
			 * 2 -- Load string immediate
			 * 3 -- Return
			 * 4 -- Load Int32 immediate (LE)
			 * 5 -- Store local variable
			 * 6 -- Branch to absolute UAL offset (32-bit offset)
			 * 7 -- Load local variable
			 * 8 -- Add
			 * 9 -- Branch on <= (ble!!!)
			 * 10 -- NOPE! You're not gonna do anything here!
			 * 11 -- Branch on equal
			 * 12 -- Branch on not equal
			 * 13 -- Branch on greater than
			 * 14 -- Branch on greater than or equal to
			 * 15 -- Subtract
			 * 16 -- Multiply
			 * 17 -- Divide
			 * 18 -- Remainder
			 * 19 -- Shift Democrat
			 * 20 -- Shift Republican
			 * 21 -- And
			 * 22 -- Or
			 * 23 -- Xor
			 * 24 -- NOT
			 * 25 -- Load immediate ON THE DOUBLE!
			 * 26 -- Load buffer immediate
			 * 255 -- End of code segment
			 * */

			Console.WriteLine ("Entering " + info.FullName);
			MemoryStream mstream = new MemoryStream ();
			BinaryWriter mwriter = new BinaryWriter (mstream);
			if (info.Body.Instructions.Count == 0 || info.IsConstructor) { //TODO: Constructor support (maybe?)
				mwriter.Write (false); //Native method invocation
				return mstream.ToArray ();
			}
			mwriter.Write (true); //Is .NET method?
			
			//Body of method
			var body = info.Body;

			//Variable count

			mwriter.Write (body.Variables.Count);

			foreach (var et in body.Variables) {
				mwriter.WriteString (et.VariableType.FullName);
			}

			//IL code for method
			var instructions = body.Instructions;

			Dictionary<int,int> UALOffsetTable = new Dictionary<int, int> (); 
			//List of code positions needing UAL offsets
			List<Tuple<int,long>> pendingLocations = new List<Tuple<int, long>> ();


			int InstructionStartPoint = (int)mstream.Position;

			///<summary>Emits a UAL OPCODE</summary>
			/// <param name="opcode">The OPCODE to emit</param>
			code_emit emitInstruction = delegate(Instruction il,byte opcode) {
				UALOffsetTable[il.Offset] = (int)mstream.Position;
				mwriter.Write(opcode);
			};


			foreach (var et in instructions) {
				Console.WriteLine (et);

				if ((int)et.OpCode.Code >= (int)Mono.Cecil.Cil.Code.Ldarg_0 && ((int)et.OpCode.Code <= (int)Mono.Cecil.Cil.Code.Ldarg_3)) {
					int argnum = 3-((int)Code.Ldarg_3 - (int)et.OpCode.Code);
					emitInstruction(et,0); //Load argument
					mwriter.Write (argnum); //Argument number
					continue;
				} 
				if ((int)et.OpCode.Code >= (int)Code.Ldc_I4_0 && (int)et.OpCode.Code <= (int)Code.Ldc_I4_8) {
					int lval = 8 - ((int)Code.Ldc_I4_8 - (int)et.OpCode.Code);
					emitInstruction (et,(byte)4);
					mwriter.Write (lval);
					continue;
				}
				if ((int)et.OpCode.Code >= (int)Code.Stloc_0 && (int)et.OpCode.Code <= (int)Code.Stloc_3) {
					int lval = 3 - ((int)Code.Stloc_3 - (int)et.OpCode.Code);
					emitInstruction (et,(byte)5);
					mwriter.Write (lval);
					continue;
				}
				if (et.OpCode.Code == Code.Stloc_S) {
					emitInstruction (et, (byte)5);
					mwriter.Write ((et.Operand as VariableReference).Index);
					continue;
				}
				if ((int)et.OpCode.Code >= (int)Code.Ldloc_0 && (int)et.OpCode.Code <= (int)Code.Ldloc_3) {
					int lval = 3 - ((int)Code.Ldloc_3 - (int)et.OpCode.Code);
					emitInstruction (et,(byte)7);
					mwriter.Write (lval);
					continue;
				}
				if (et.OpCode.Code == Code.Ldloc_S) {
					emitInstruction (et, (byte)7);
					mwriter.Write ((et.Operand as VariableReference).Index);
					continue;
				}
					switch (et.OpCode.Code) {
					case Mono.Cecil.Cil.Code.Call:
						{
							var method = et.Operand as MemberReference; //Method to invoke
							emitInstruction(et,(byte)1);
							mwriter.Write (GetMethodPointer (method.FullName));
						}
						break;
					case Mono.Cecil.Cil.Code.Ldstr:
						emitInstruction (et,(byte)2);
						mwriter.WriteString (et.Operand as string);
						break;
				case Mono.Cecil.Cil.Code.Nop:
					emitInstruction (et, 10);
						break;
					case Code.Ret:
						//Return from function call
						emitInstruction (et,(byte)3);
						break;
				case Code.Br:
					emitInstruction (et, 6);
					pendingLocations.Add (new Tuple<int, long> ((et.Operand as Instruction).Offset, mstream.Position));
					mwriter.Write ((int)0);
					break;
				case Code.Add:
					emitInstruction (et, 8);
					break;
				case Code.Ldc_I4_S:
					//Load int immediate
					emitInstruction (et, 4);
					mwriter.Write ((int)(sbyte)et.Operand);
					break;
				case Code.Ldc_I4:
					//Load int immediate
					emitInstruction (et, 4);
					mwriter.Write ((int)et.Operand);
					break;
				case Code.Ble:
					emitInstruction (et, 9);
					pendingLocations.Add (new Tuple<int, long> ((et.Operand as Instruction).Offset, mstream.Position));
					mwriter.Write ((int)0);
					break;
				case Code.Beq:
					emitInstruction (et, 11);
					pendingLocations.Add (new Tuple<int, long> ((et.Operand as Instruction).Offset, mstream.Position));
					mwriter.Write ((int)0);
					break;
				case Code.Bne_Un:
					emitInstruction (et, 12);
					pendingLocations.Add (new Tuple<int, long> ((et.Operand as Instruction).Offset, mstream.Position));
					mwriter.Write ((int)0);
					break;
				case Code.Bgt:
					emitInstruction (et, 13);
					pendingLocations.Add (new Tuple<int, long> ((et.Operand as Instruction).Offset, mstream.Position));
					mwriter.Write ((int)0);
					break;
				case Code.Bge:
					emitInstruction (et, 14);
					pendingLocations.Add (new Tuple<int, long> ((et.Operand as Instruction).Offset, mstream.Position));
					mwriter.Write ((int)0);
					break;
				case Code.Sub:
					emitInstruction (et, 15);
					break;
				case Code.Mul:
					emitInstruction (et, 16);
					break;
				case Code.Div:
					emitInstruction (et, 17);
					break;
				case Code.Rem:
					emitInstruction (et, 18);
					break;

					//Binary instructions

				case Code.Shl:
					emitInstruction (et, 19);
					break;
				case Code.Shr:
					emitInstruction (et, 20);
					break;
				case Code.And:
					emitInstruction (et, 21);
					break;
				case Code.Or:
					emitInstruction (et, 22);
					break;
				case Code.Xor:
					emitInstruction (et,23);
					break;
				case Code.Not:
					emitInstruction (et, 24);
					break;
				case Code.Ldc_R8:
					emitInstruction (et, 25);
					mwriter.Write ((double)et.Operand);
					break;

					default:
						Console.WriteLine ("Unknown OPCODE: " + et.OpCode);
						break;
					}




			}
			mwriter.Write ((byte)255);
			//Update everything that needs a UAL offset
			foreach (var et in pendingLocations) {
				mstream.Position = et.Item2;
				mwriter.Write (UALOffsetTable [et.Item1]-InstructionStartPoint);
			}
			return mstream.ToArray ();
		}

	}

	public class UALType:UALSerializable {
		TypeDefinition t;
		public UALType(TypeDefinition t) {
			this.t = t;
		}
		public override byte[] Serialize ()
		{
			MemoryStream mstream = new MemoryStream ();
			BinaryWriter mwriter = new BinaryWriter (mstream);
			mwriter.Write (t.Methods.Count);
			foreach (var et in t.Methods) {
				//Name of method
				mwriter.WriteString (et.FullName);
				byte[] data = new UALMethod (et).Serialize();
				mwriter.Write (data.Length);
				mwriter.Write (data);


			}


			//Write all fields
			mwriter.Write (t.Fields.Count);
			foreach (var et in t.Fields) {
				mwriter.WriteString (et.Name);
				mwriter.Write (new UALField (et).Serialize ());
			}

			return mstream.ToArray ();
		}
	}
	public class UALModule:UALSerializable
	{
		AssemblyDefinition assembly;
		public UALModule(Mono.Cecil.AssemblyDefinition assembly)
		{
			this.assembly = assembly;
		}

		public override byte[] Serialize ()
		{
			MemoryStream mstream = new MemoryStream ();
			BinaryWriter mwriter = new BinaryWriter (mstream);
			//Export data types
			var module = assembly.MainModule;
			mwriter.Write (module.Types.Count);
			foreach (var et in module.Types) {
				//Name of data type
				mwriter.WriteString (et.FullName);
				//Serialized form of data type, prefixed with length for fast-load (we will load and compile 1 class at a time)
				byte[] data = new UALType (et).Serialize ();
				mwriter.Write (data.Length);
				mwriter.Write (data);

			}

			
			//Export methods import table
			mwriter.Write (UALMethod.MethodTable.Count);
			foreach (var et in UALMethod.MethodTable) {
				mwriter.Write (et.Value);
				mwriter.WriteString (et.Key);
			}

			return mstream.ToArray ();
		}

	}
}

