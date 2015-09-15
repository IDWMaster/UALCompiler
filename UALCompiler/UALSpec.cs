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
		MethodDefinition info;
		public UALMethod(MethodDefinition info) {
			this.info = info;
		}
		public override byte[] Serialize ()
		{
			MemoryStream mstream = new MemoryStream ();
			BinaryWriter mwriter = new BinaryWriter (mstream);
			//Body of method
			var body = info.Body;
			//IL code for method
			var instructions = body.Instructions;
			mwriter.Write (instructions.Count);
			foreach (var et in instructions) {
				if (et.OpCode.Code == Mono.Cecil.Cil.Code.Ldarg) {
					//TODO: Load arguement
					throw new NotImplementedException ("TODO: Load argument instruction");

				}
			}
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
				mwriter.Write (new UALMethod (et).Serialize ());

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

