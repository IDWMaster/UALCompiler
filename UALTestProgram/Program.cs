namespace UALTestProgram
{
	class MainClass
	{


		public static extern void ConsoleOut (string txt);
		public static extern void PrintInt (int value);
		public static extern void PrintDouble (double value);

		//Recursive addition of unsigned ints
		static int recursiveAdd(int a, int b) {
			if (a > 0) {
				return recursiveAdd (a - 1, b) + 1;
			}
			return b;
		}



		public static void Main (string[] args)
		{


			string tab = "\t";
			string newline = "\n";

			ConsoleOut ("==================================================================\nUAL test program\nCopyright 2016 Brian Bosak (GPLv3.0 license)\n==================================================================\nThis program is designed to test the UAL compiler and runtime environment. Output may vary slightly by host platform.\n\n");

			//PrintInt (dosomething ()); //Function call test
			//ConsoleOut (newline);
			ConsoleOut ("5+2 (recursively) = ");
			PrintInt(recursiveAdd (5, 2));
			ConsoleOut ("\n");





			double a = 8.2;
			double b = 5.9;
			double answer = a + b;
			ConsoleOut ("8.2+5.9 ~= ");
			PrintDouble (answer);
			ConsoleOut (newline);
			answer = a - b;
			ConsoleOut ("8.2-5.9 ~= ");
			PrintDouble (answer);
			ConsoleOut (newline);
			answer = a * b;
			ConsoleOut ("8.2*5.9 ~= ");
			PrintDouble (answer);
			ConsoleOut (newline);
			answer = a / b;
			ConsoleOut ("8.2/5.9 ~= ");
			PrintDouble (answer);
			ConsoleOut (newline);

			int x = 5;
			int y = 2;
			int result = x + y;
			ConsoleOut ("5+2 = ");
			PrintInt (result);
			ConsoleOut (newline);




			ConsoleOut ("A multiplication table should be printed below: \n\n\n");


			for (int i = 1; i <= 12; i++) {
				for (int c = 1; c <= 12; c++) { //This is how C++ was invented
					PrintInt (c*i);
					ConsoleOut (tab);
				}
				ConsoleOut (newline);
			}
			ConsoleOut (newline);
		}

	}
}
