using OpenPop.Mime;
using OpenPop.Common;
using System;
using System.Collections.Generic;

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
                WindowsRemoteManagerExecutive WRM = new WindowsRemoteManagerExecutive("pop.mail.ru", "windows.manager@bk.ru", "1238Rjhjkm16", 995, @"C:\Users\Public\Pictures");
                WRM.Launch();
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
