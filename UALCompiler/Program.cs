using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
namespace UALCompiler
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			if (System.Diagnostics.Debugger.IsAttached) {
				args = new string[] { "UALTestProgram.exe" };
			}

			if (args.Length == 0) {
				Console.WriteLine ("Error -- No input files specified. For usage information, run UALCompiler --help");
			} else {
				if (args.Contains ("--help")) {
					Console.WriteLine ("Usage: UALCompiler file [options]");
					Console.WriteLine ("Options: ");
					Console.WriteLine ("\t--help\t\t\tGets help for this program");
				} else {
					//Read args[0] as input file
					string inputFile = args [0];
					//Load input assembly
					try {
						var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(inputFile);
					//Generate UAL stream
						var ualassembly = new UALModule(assembly);
						using(Stream str = File.Create("ual.out")) {
							byte[] data = ualassembly.Serialize();
							str.Write(data,0,data.Length);
						}
					}catch(Exception er) {
						Console.WriteLine ("Error: Unable to read the specified IL assembly file.");
					}
				}
			}
		}
	}
}
