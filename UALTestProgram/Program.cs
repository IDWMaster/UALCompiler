using System;
using System.Runtime.InteropServices;
namespace UALTestProgram
{
	class MainClass
	{
		[DllImport("runtime")]
		public static extern void ConsoleOut (string txt);


		public static void Main (string[] args)
		{
			ConsoleOut ("Hello World!\n");
		}
	}
}
