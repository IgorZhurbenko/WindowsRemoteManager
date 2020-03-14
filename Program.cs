using System;


namespace WindowsRemoteManager
{

    
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Type 1 to launch Windows remote executive and 2 to launch Windows remote master.");
            Char key = Console.ReadKey().KeyChar;
            Console.WriteLine();

            if (key == '1')
            {
                WindowsRemoteManagerExecutive WRME = new WindowsRemoteManagerExecutive("pop.mail.ru", "windows.manager@bk.ru", "1238Rjhjkm16", 995, @"C:\Users\Public\Pictures");
                WRME.Launch();
            }
            else if (key == '2')
            {
                WindowsRemoteManagerMaster WRMM = new WindowsRemoteManagerMaster("pop.mail.ru", "windows.manager@bk.ru", "1238Rjhjkm16", 995, @"C:\Users\Public\Pictures");
                WRMM.Launch();
            }
            else
            {
                Console.WriteLine("Wrong symbol. Try again.");
                Main(args);
            }
        }
    }
}
