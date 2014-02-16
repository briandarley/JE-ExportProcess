﻿using System;
using System.Configuration;
using System.Net;
using System.Text;
using Jscape.Ftp;

namespace ExportProcess
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {

                var records = ESpeed.Records.GetRecords(new ESpeed.Records.Criteria(ESpeed.Types.DocStatusTypes.HealthE_Waiting));



                if (records != null && records.ESpeedRecords != null)
                {
                    records.FileCopiedEventHandler += OnFileCopied;

                    var recordCount = records.ESpeedRecords.Count.ToString().PadLeft(5, '0');

                    var zipFile = GetZipFileName(recordCount);

                    Console.WriteLine("Adding records to ziped file");
                    AddRecordsToZipFile(records, zipFile);

                    Console.WriteLine("Adding control file to zip file");
                    AddSourceControlFileToZipFile(records, zipFile);

                    Console.WriteLine("Submitting zipped file to FTP");
                    SubmitZipFileToVendorViaFtp(zipFile);


                    Console.WriteLine("Committing changes to database");

                    records.UpdateStatus();


                    SendCompleteNotification();

                    Console.WriteLine("\nDone copying files");
                }
                else
                {
                    Console.WriteLine("Process Ran Successfully, there were no files to process");
                }

            }
            catch (Exception ex)
            {
                SendErrorNotification(ex);
            }


        }

      

        private static void TestFTPConnection()
        {
            var ftp = RetrieveInitializedFtpObject();
            ftp.Connect();
            //ftp.DownloadDir(ConfigurationManager.AppSettings["FTPDestinationFolder"]);

            ftp.RemoteDir = ConfigurationManager.AppSettings["FTPDestinationFolder"];
            ftp.LocalDir = @"c:\temp\";

            ftp.Upload(@"test.txt");


            ftp.Disconnect();
            ftp.DebugStream.Close();

        }



        private static void SubmitZipFileToVendorViaFtp(string zipFile)
        {
            var ftp = RetrieveInitializedFtpObject();
            ftp.Connect();

            ftp.RemoteDir = ConfigurationManager.AppSettings["FTPDestinationFolder"];
            ftp.LocalDir = System.IO.Path.GetDirectoryName(zipFile);

            ftp.Upload(System.IO.Path.GetFileName(zipFile));

            ftp.Disconnect();
            if (DebugFtp())
                ftp.DebugStream.Close();



        }



        private static void AddSourceControlFileToZipFile(ESpeed.Records records, string zipFileName)
        {
            var zipFile = new Ionic.Zip.ZipFile(zipFileName);

            var controlFileName = string.Format("imageControl_Out_{0}.csv", DateTime.Now.ToString("yyyyMMddhhmmss"));
            zipFile.AddEntry(controlFileName, records.GenerateOutput(true));
            zipFile.Save();
        }

        private static void AddRecordsToZipFile(ESpeed.Records records, string zipFileName)
        {
            records.AddSourceFilesToZipArchive(zipFileName);
        }

        private static string GetZipFileName(string recordCount)
        {
            var path = ConfigurationManager.AppSettings["Outputfolder"];
            return System.IO.Path.Combine(path, string.Format("PBM-JECO-{0}{1}.zip", DateTime.Now.ToString("yyyyMMdd"), recordCount));
        }

        static void OnFileCopied(object sender, EventArgs e)
        {
            Console.Write(".");
        }

        private static Ftp RetrieveInitializedFtpObject()
        {
            //Secure FTP Factory for .NET:Single Developer:Registered User:01-01-3999:I5eiUi3WpB4FspHQ5uOlKRh2eabOrFtcMbh74FmX/UzFUolDU9f5hbostB6xBnB9Xq8sNOFDx7jmiPkXbKhpZw4k0sWKlSywRceV4ir8csHWdBYdmFkrOSY+eYgjp+ud9GsP+m8sO1dun1qlzBMW87Fkpsmgbs88W7USXkMBl/U=

            var host = ConfigurationManager.AppSettings["FTPAddress"];
            var user = ConfigurationManager.AppSettings["FTPUserId"];
            var pwd = ConfigurationManager.AppSettings["FTPPassword"];

            var ftp = new Ftp(host, user, pwd)
                      {
                          LicenseKey = ConfigurationManager.AppSettings["FTPLicenseKey"],
                          ConnectionType = Ftp.DEFAULT,
                          Debug = DebugFtp()

                      };
            if (DebugFtp())
            {
                var path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                ftp.DebugStream = System.IO.File.CreateText(System.IO.Path.Combine(path, "FTP_DEBUG.LOG"));
            }

            return ftp;
        }

        private static bool DebugFtp()
        {
            return (ConfigurationManager.AppSettings["FTPDebug"] == "true" ? true : false);
        }

        private static void SendErrorNotification(Exception exception)
        {

            var sb = new StringBuilder();
            sb.Append("Description of error\n\n");
            sb.Append(exception.Message);
            sb.Append("\n\n");
            if (exception.InnerException != null)
            {
                sb.Append("Inner exception\n\n");
                sb.Append(exception.InnerException.Message);
                sb.Append("\n\n");
            }
            sb.Append("Stack tract\n\n");
            sb.Append(exception.StackTrace);


            SendEmailNotification("Failure - FTP process to HealthESystems ",
                  sb.ToString()); 
   

        }
        
        private static void SendCompleteNotification()
        {
            SendEmailNotification("HealthESystems Success",
                "File successfully submitted to HealthESystems");
        }

        private static void SendEmailNotification(string subject, string messageBody)
        {
            var smtp = new System.Net.Mail.SmtpClient("shire")
                         { 
                             Credentials = new NetworkCredential(ConfigurationManager.AppSettings["emailAuthenPWD"],ConfigurationManager.AppSettings["emailAuthenUID"])
                         };
            var msg = new System.Net.Mail.MailMessage
                      {
                          From =
                              new System.Net.Mail.MailAddress(ConfigurationManager.AppSettings["SourceEmail"])

                      };


            foreach (var recipient in ConfigurationManager.AppSettings["emailNotification"].Split(','))
            {
                msg.To.Add(recipient);
            }

            msg.Subject = subject;
            msg.Body = messageBody;
            smtp.Send(msg);

        }

    }
}
