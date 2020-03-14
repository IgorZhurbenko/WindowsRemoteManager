using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using OpenPop.Pop3;
using System.Linq;
using System.Threading;
using System.Net.Mail;
using System.Net;

namespace WindowsRemoteManager
{
    class WindowsRemoteManagerExecutive : WindowsRemoteManagerGeneral
    {
        
        public string NickName = "NickName";
        public WindowsRemoteManagerExecutive(string ServerAddress, string Login, string Password, int Port, string BaseFolder)
            : base(ServerAddress, Login, Password, Port, BaseFolder)
        {
           
        }
        
        private List<string> ExecuteBat(Command command)
        {

            List<string> result = new List<string>();
            string BatContent = "";

            foreach (string Instruction in command.Instructions)
            {
                if (Instruction.StartsWith("{"))
                {
                    BatContent = BatContent + '\n' + Instruction.Replace("{", "").Replace("}","");
                }
                else if (Instruction.EndsWith("}") || Instruction.EndsWith("}\r"))
                {
                    BatContent = BatContent + '\n' + Instruction.Replace("}", "").Replace("{","");
                    break;
                }
                else 
                {
                    BatContent = BatContent + '\n' + Instruction;
                }
            }

            string FileName = this.BaseFolder + @"\" + "Command " + this.ID.ToString() + " " + command.ID.ToString() + ".bat";

            if (File.Exists(FileName)) { File.Delete(FileName); }

            File.AppendAllText(FileName, BatContent);

            ProcessStartInfo psiOpt = new ProcessStartInfo(@FileName);
            psiOpt.WindowStyle = ProcessWindowStyle.Hidden;
            psiOpt.RedirectStandardOutput = true;
            psiOpt.UseShellExecute = false;
            psiOpt.CreateNoWindow = true;
            Process procCommand = Process.Start(psiOpt);
            StreamReader sr = procCommand.StandardOutput;
            result.Add(sr.ReadToEnd());
            File.Delete(FileName);
            return result;
        }
        
        private List<string> Execute(Command command)
        {
            List<string> result = new List<string>();

            if (command.Instructions[0].StartsWith("{"))
            {
                return this.ExecuteBat(command);
            }

            foreach (string Instruction in command.Instructions)
            {
                
                if (!Instruction.StartsWith(@"'") || !Instruction.StartsWith(@"{"))
                {
                    ProcessStartInfo psiOpt = new ProcessStartInfo(@"cmd.exe", @"/C " + @Instruction);
                    psiOpt.WindowStyle = ProcessWindowStyle.Hidden;
                    psiOpt.RedirectStandardOutput = true;
                    psiOpt.UseShellExecute = false;
                    psiOpt.CreateNoWindow = true;
                    Process procCommand = Process.Start(psiOpt);
                    StreamReader sr = procCommand.StandardOutput;
                    result.Add(sr.ReadToEnd());
                }
                else if (Instruction.StartsWith(@"'curl"))
                {
                    ProcessStartInfo psiOpt = new ProcessStartInfo(@"curl.exe", /*@"/C " +*/ @Instruction.Replace(@"'curl", ""));
                    psiOpt.WindowStyle = ProcessWindowStyle.Hidden;
                    psiOpt.RedirectStandardOutput = true;
                    psiOpt.UseShellExecute = false;
                    psiOpt.CreateNoWindow = true;
                    Process procCommand = Process.Start(psiOpt);
                    StreamReader sr = procCommand.StandardOutput;
                    result.Add(sr.ReadToEnd());
                }
                else if (Instruction.StartsWith(@"'SetRequestsInterval"))
                {
                    string IntervalString = Instruction.Split(" ")[1].Trim();
                    try
                    {
                        int Interval = Convert.ToInt32(IntervalString);
                        if (Interval < 100) { Interval = 100; }
                        if (Interval > 3600000) { Interval = 3600000; }
                        this.RequestsInterval = Interval;
                        result.Add("Requests interval set to " + Interval.ToString());
                    }
                    catch { result.Add("Wrong input for requests interval"); }

                }
            }
            return result;
        }
      
        private List<Command> GetCommands()
        {            
            if (!this.PClient.Connected) { return new List<Command>(); }
            List<Command> Commands = new List<Command>();
            OpenPop.Pop3.Pop3Client PClient = this.PClient;
            int i;
            int n;
            try { n = PClient.GetMessageCount(); }
            catch { Console.WriteLine("Stream has been unexpectedly closed. Reconnecting..."); this.ConnectReceive(); this.ConnectSend(); return GetCommands(); }
            for (i = n; i >= 1; i--)
            {
                try
                {
                    if (PClient.GetMessageHeaders(i).Subject.StartsWith("Command " + this.ID.ToString()))
                    {
                        Int64 ID = Convert.ToInt64(PClient.GetMessageHeaders(i).Subject.Replace("Command " + this.ID.ToString() + " ", ""));
                        List<string> Instructions = PClient.GetMessage(i).FindFirstPlainTextVersion().GetBodyAsText().Split('\n').ToList();
                        Commands.Add(new Command(ID, Instructions));
                        PClient.DeleteMessage(i);
                    }
                }
                catch { Console.WriteLine("Stream has been unexpectedly closed. Reconnecting..."); this.ConnectReceive(); this.ConnectSend(); return GetCommands(); }
                
            }
            PClient.Disconnect();
            this.ConnectReceive();
            return Commands;
        }

        public string GetLogFile()
        {
            return this.BaseFolder + @"/log.txt";
        }

        private string GetMac()
        {
            ProcessStartInfo psiOpt = new ProcessStartInfo(@"cmd.exe", @"/C " + "getmac");
            psiOpt.WindowStyle = ProcessWindowStyle.Hidden;
            psiOpt.RedirectStandardOutput = true;
            psiOpt.UseShellExecute = false;
            psiOpt.CreateNoWindow = true;
            Process procCommand = Process.Start(psiOpt);
            StreamReader sr = procCommand.StandardOutput;
            return sr.ReadToEnd().Split('\n')[3].Split(" ")[0];
        }
        
        private void LoopAction()
        {
            foreach (Command command in this.GetCommands())
            {
                File.AppendAllText(this.GetLogFile(), @"***" + command.ID.ToString() + "***" + '\n');
                List<string> results = this.Execute(command);
                string message = "";
                foreach (string result in results)
                {
                    File.AppendAllText(this.GetLogFile(), result);
                    message = message + result + '\n';
                }
                this.Send("Report " + this.ID.ToString() + " " + command.ID.ToString(), message);
            }
        }

        public void Launch()
        {
            this.ConnectReceive();
            this.ConnectSend();
            this.RegisterExecutive();

            Console.WriteLine("Connection set. Executing commands...");
            while (true)
            {
                this.LoopAction();
                Thread.Sleep(RequestsInterval);
            }
        }

        private void RegisterExecutive()
        {
            string Executives = GetMessageBodyByHeader("Executives", true);
            if (Executives != "Message not found")
            {
                string[] ExecutivesTable = Executives.Split('\n');
                Int64[] IDs = new Int64[ExecutivesTable.Count() - 1];
                string Mac = GetMac();
                for (int i = 1; i < ExecutivesTable.Count(); i++)
                {
                    string[] Row = ExecutivesTable[i].Split("|");
                    IDs[i - 1] = Convert.ToInt64(Row[1]);
                    if (Row[0].Contains(Mac))
                    {
                        this.ID = Convert.ToInt64(Row[1]);
                        ExecutivesTable[i] = ExecutivesTable[i].Replace(Row[2], this.NickName).Replace(Row[3], DateTime.Now.ToString() );
                        this.Send("Executives", String.Join('\n', ExecutivesTable));
                        
                        return;
                    }
                }

                Int64 PickedID = 1;
                while (IDs.Contains(PickedID))
                {
                    PickedID++;
                }

                string NewRow = Mac + "|" + PickedID.ToString() + "|" + this.NickName + "|" + "CurrentDate";
                string NewExecutivesInfo = String.Join('\n', ExecutivesTable) + '\n' + NewRow;
                this.ID = PickedID;
                this.Send("Executives", NewExecutivesInfo);
                return;

            }

            string Message = "Mac|ID|NickName|RegistrationDate\n" + GetMac() + "|" + "1" + "|" + this.NickName + "|" + "CurrentDate";
            this.ID = 1;
            this.Send("Executives", Message);

        }
    }
}
