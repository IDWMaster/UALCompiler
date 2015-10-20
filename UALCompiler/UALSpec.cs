using System;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
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
		//Table of all imports
		static Dictionary<string,int> MethodTable = new Dictionary<string, int>();
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
			Console.WriteLine ("Entering " + info.FullName);
			MemoryStream mstream = new MemoryStream ();
			BinaryWriter mwriter = new BinaryWriter (mstream);
			if (info.HasPInvokeInfo) {
				mwriter.Write (false); //Native method invocation
				return mstream.ToArray ();
			}
			mwriter.Write (true); //Is .NET method?


			//Body of method
			var body = info.Body;
			//IL code for method
			var instructions = body.Instructions;


			mwriter.Write (instructions.Count);
			foreach (var et in instructions) {
				if ((int)et.OpCode.Code >= (int)Mono.Cecil.Cil.Code.Ldarg_0 && ((int)et.OpCode.Code <= (int)Mono.Cecil.Cil.Code.Ldarg_3)) {
					//TODO: Load arguement
					int argnum = (int)Mono.Cecil.Cil.Code.Ldarg_0-(int)et.OpCode.Code;
					mwriter.Write ((byte)0); //Load argument
					mwriter.Write (argnum); //Argument number
				} else {
					if (et.OpCode.Code == Mono.Cecil.Cil.Code.Call) {
						var method = et.Operand as MemberReference; //Method to invoke
						mwriter.Write ((byte)1);
						mwriter.Write (GetMethodPointer (method.FullName));

					} else {
						if (et.OpCode.Code == Mono.Cecil.Cil.Code.Ldstr) {
							mwriter.Write ((byte)2);
							mwriter.WriteString (et.Operand as string);


						} else {
							if (et.OpCode.Code == Mono.Cecil.Cil.Code.Nop) {
								//TODO: Nothing.
							} else {
								if (et.OpCode.Code == Mono.Cecil.Cil.Code.Ret) {
									//Return from function call
									mwriter.Write ((byte)3);
								} else {
									Console.WriteLine ("Not implemented: " + et.OpCode);
								}
							}
						}
					}
				}
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
			//Write all fields
			mwriter.Write (t.Fields.Count);
			foreach (var et in t.Fields) {
				mwriter.WriteString (et.Name);
				mwriter.Write (new UALField (et).Serialize ());
			}
			mwriter.Write (t.Methods.Count);
			foreach (var et in t.Methods) {
				//Name of method
				mwriter.WriteString (et.Name);
				byte[] data = new UALMethod (et).Serialize();
				mwriter.Write (data.Length);
				mwriter.Write (data);


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
			return mstream.ToArray ();
		}

	}
}

