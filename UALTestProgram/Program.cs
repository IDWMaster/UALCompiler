using System;
using System.Runtime.InteropServices;
namespace UALTestProgram
{
	class MainClass
	{


		[DllImport("runtime")]
		public static extern void ConsoleOut (string txt);
		[DllImport("runtime")]
		public static extern void PrintInt (int value);

		public static void Main (string[] args)
		{
			//ConsoleOut ("Hello World!\n");

			for (int i = 0; i != 10; i++) {
				PrintInt (i);
				ConsoleOut ("\n");
			}
		}
	}
}
