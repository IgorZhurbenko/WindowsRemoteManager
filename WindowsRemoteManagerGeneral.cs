using System;
using OpenPop.Pop3;
using System.Net.Mail;
using System.Net;



abstract class WindowsRemoteManagerGeneral

{
    protected string BaseFolder = "";
    protected Int64 ID = 0;
    protected OpenPop.Pop3.Pop3Client PClient;
    protected string Login; protected int Port; protected string Password;
    protected string ServerAddress;
    protected SmtpClient SClient;
    protected int RequestsInterval = 100;

    protected WindowsRemoteManagerGeneral(string ServerAddress, string Login, string Password, int Port, string BaseFolder)
    {
        this.ServerAddress = ServerAddress;
        this.Login = Login;
        this.Password = Password;
        this.Port = Port;
        this.BaseFolder = BaseFolder;
    }

    public bool ConnectReceive()
    {
        OpenPop.Pop3.Pop3Client PClient = new Pop3Client();
        PClient.Connect(this.ServerAddress, this.Port, true);
        PClient.Authenticate(this.Login, this.Password, AuthenticationMethod.UsernameAndPassword);
        if (PClient.Connected) { this.PClient = PClient; }
        return PClient.Connected;
    }

    public bool ConnectSend()
    {
        try
        {
            this.SClient = new SmtpClient("smtp.mail.ru", 25);
            this.SClient.Credentials = new NetworkCredential(this.Login, this.Password);
            this.SClient.EnableSsl = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected void Send(string Subject, string Body)
    {
        MailAddress from = new MailAddress(this.Login);
        MailAddress to = new MailAddress(this.Login);
        MailMessage m = new MailMessage(from, to);
        m.Subject = Subject;
        m.Body = Body;
        
        try
        {
            this.SClient.Send(m);
        }
        catch (OpenPop.Pop3.Exceptions.InvalidLoginException error1) { throw new OpenPop.Pop3.Exceptions.InvalidLoginException(error1); }
        catch (OpenPop.Pop3.Exceptions.PopServerNotFoundException error2) { throw new OpenPop.Pop3.Exceptions.PopServerNotFoundException(error2.Message, error2); }
        catch { this.ConnectSend(); Send(Subject, Body); }
    }

    protected string GetMessageBodyByHeader(string SeekedHeader, bool DeleteAfterGetting = true)
    {
        string result = "Message not found";

        this.ConnectReceive();
        OpenPop.Pop3.Pop3Client PClient = this.PClient;
        int i;
        int n = PClient.GetMessageCount();
        for (i = n; i >= 1; i--)
        {
            string header = PClient.GetMessageHeaders(i).Subject;
            if (header.StartsWith(SeekedHeader))
            {
                result = PClient.GetMessage(i).FindFirstPlainTextVersion().GetBodyAsText();
                if (DeleteAfterGetting) 
                { 
                    PClient.DeleteMessage(i); 
                    PClient.Disconnect();
                    this.ConnectReceive();
                }
                
                break;
            }

        }

        return result.Trim();
    }
    
}


