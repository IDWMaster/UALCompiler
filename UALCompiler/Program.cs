using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UALCompiler
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			if (args.Length == 0) {
				Console.WriteLine ("Error -- No input files specified. For usage information, run UALCompiler --help");
			} else {
				if (args.Contains ("--help")) {
					Console.WriteLine ("Usage: UALCompiler file [options]");
					Console.WriteLine ("Options: ");
					Console.WriteLine ("\t--help\t\t\tGets help for this program");
					Console.WriteLine ("\t--output filename\tSpecifies output assembly file");
				} else {
					//Read args[0] as input file
					string inputFile = args [0];
					//Load input assembly
					try {
						var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(inputFile);
					//Generate UAL stream

					}catch(Exception er) {
						Console.WriteLine ("Error: Unable to read the specified IL assembly file.");
					}
				}
			}
		}
	}
}
