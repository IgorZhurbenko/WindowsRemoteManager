using System;
using OpenPop.Pop3;
using System.Threading;
using System.Net.Mail;
using System.Net;
using WindowsRemoteManager;

namespace WindowsRemoteManager
{
    class WindowsRemoteManagerMaster : WindowsRemoteManagerGeneral
    {
        public WindowsRemoteManagerMaster(string ServerAddress, string Login, string Password, int Port, string BaseFolder)
            : base(ServerAddress, Login, Password, Port, BaseFolder)
        {

        }

        private string GetReport(Int64 CommandID)
        {
            try
            {
                string report = "";
                do { report = GetMessageBodyByHeader("Report " + this.ID.ToString() + " " + CommandID.ToString()); }
                while (report == "Message not found");
                return report;
            }
            catch
            {
                this.ConnectReceive();
                return GetReport(CommandID);
            }
        }

        private string RecordInstruction()
        {
            string EnteredLine;
            bool RecordingBat = false;
            string instruction = "";

            do
            {
                EnteredLine = Console.ReadLine();
                RecordingBat = (RecordingBat || EnteredLine.StartsWith("{")) && !EnteredLine.EndsWith("}");
                instruction = instruction + EnteredLine + '\n';
            }
            while (RecordingBat);

            return instruction;
        }

        private bool LoopAction(int IterationNumber, string Instruction)
        {

            try
            {
                this.Send("Command " + this.ID.ToString() + " " + IterationNumber.ToString(), Instruction);

                Thread.Sleep(RequestsInterval + 100);

                string report = GetReport(IterationNumber);

                Console.WriteLine(report);

                return true;
            }
            catch { return LoopAction(IterationNumber, Instruction); }

        }

        private void ClearAllReports()
        {
            int i = 1;
            int n = PClient.GetMessageCount();
            for (i = n; i >= 1; i--)
            {
                string header = PClient.GetMessageHeaders(i).Subject;
                if (header.StartsWith("Report " + this.ID.ToString()))
                {
                    PClient.DeleteMessage(i);
                }

            }
            PClient.Disconnect();
            this.ConnectReceive();
        }
        
        public void Launch()
        {

            this.ConnectReceive();
            this.ConnectSend();
            string RegisteredIDs = GetMessageBodyByHeader("Executives", false);
            if (RegisteredIDs != "Message not found")
            {
                Console.WriteLine(RegisteredIDs);
                Console.WriteLine("Connection set. Here is the table of registered executives. Choose ID of the one you want to manage.");
            }

            else
            {
                Console.WriteLine("There are no registered executives. It is unlikely that any executive is operational now\n " +
                    "You may still enter an ID");
            }

            bool error = true;
            while (error)
            { 
                try { this.ID = Convert.ToInt64(Console.ReadLine()); error = false; }
                catch { Console.WriteLine("Invalid value entered. Try only numbers this time."); }
            }
            ClearAllReports();

            int i = 1;

            while (true)
            {
                string instruction = RecordInstruction();
                LoopAction(i, instruction);
                i++;
            }
        }

    }

}

