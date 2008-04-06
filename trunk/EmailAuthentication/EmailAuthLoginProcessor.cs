//* 
//* Copyright (c) 2008 David Podhola. 
//* 
//* Permission is hereby granted, free of charge, to any person obtaining a copy
//* of this software and associated documentation files (the "Software"), to deal
//* in the Software without restriction, including without limitation the rights
//* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//* copies of the Software, and to permit persons to whom the Software is
//* furnished to do so, subject to the following conditions:
//* 
//* The above copyright notice and this permission notice shall be included in
//* all copies or substantial portions of the Software.
//* 
//* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//* THE SOFTWARE.
//* 
//
// /home/davidpodhola/Documents/dev/HawkWikiEx/Goodies/EmailAuthentication/EmailAuthLoginProcessor.cs created with MonoDevelop
// User: davidpodhola at 4:14 PM 4/6/2008
//

using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Web;
using System.Web.Security;
using HawkWiki.SampleWiki;
using NVelocity;
using DotNetOpenMail;
using DotNetOpenMail.SmtpAuth;

namespace HawkWikiEx.EmailAuthentication
{
	public class EmailAuthLoginProcessor : ILoginProcessor
	{		
		private static string emailSender = ConfigurationSettings.AppSettings[ "HawkWikiEx.EmailAuthentication.EmailSender" ]; 
		private static string emailSubject = ConfigurationSettings.AppSettings[ "HawkWikiEx.EmailAuthentication.EmailSubject" ]; 
		private static string smtpServerAddr = ConfigurationSettings.AppSettings[ "HawkWikiEx.EmailAuthentication.SmtpServer" ]; 
		private static string smtpServerUser = ConfigurationSettings.AppSettings[ "HawkWikiEx.EmailAuthentication.SmtpServerUser" ]; 
		private static string smtpServerPwd = ConfigurationSettings.AppSettings[ "HawkWikiEx.EmailAuthentication.SmtpServerPwd" ]; 
		
		public string LoginPageTemplate()
		{
			return "EmailLogin.vm";
		}
		
		public void PrepareLoginPage( HttpContext context, VelocityContext vcontext )
		{
             string email = context.Request[ "Email" ] + String.Empty;
			
	         if ( email.Length > 0 ) {			
                // send the email
                EmailMessage emailMessage = new EmailMessage();
                string sender = emailSender;
                emailMessage.FromAddress = new EmailAddress(sender);
                emailMessage.AddToAddress(new EmailAddress( email ));
                emailMessage.AddBccAddress(new EmailAddress(sender));

				emailMessage.Subject = emailSubject;
				
				Template template = WikiHandler.Velocity.GetTemplate(context.Server.MapPath("~/Templates/AuthEmail.vm"));
				StringWriter sw = new StringWriter();

                byte[] data = new byte[ 16 ];  // 16 bytes = 128 bits
                System.Security.Cryptography.RNGCryptoServiceProvider rng = new
                  System.Security.Cryptography.RNGCryptoServiceProvider();
                rng.GetBytes( data );
                Guid g = new Guid( data );

				string reqUrl = context.Request.Url.ToString();
				string fullPath = reqUrl.Substring(0,reqUrl.LastIndexOf("/")+1 );
                string loginPage = fullPath + "/ProcessDummyLogin.aspx?code=" + g.ToString();
				string hostName = context.Request.UserHostName;
				string userName = FileDataProvider.GetUserSignature( email );
				
				VelocityContext vemailctx = new VelocityContext();				vemailctx.Put("URL", fullPath );
				vemailctx.Put("LOGINLINK", loginPage );
				vemailctx.Put("HOSTNAME", hostName );
                vemailctx.Put("USERNAME", userName );
				
				template.Merge( vemailctx, sw );				
				
                string body = sw.ToString();

                // Mark the sent code
                context.Application[ "AuthCode:" + g.ToString() ] = email;

                emailMessage.HtmlPart = new HtmlAttachment( body );
                SmtpServer smtpServer=new SmtpServer(smtpServerAddr);
				if ( smtpServerUser != null && smtpServerUser.Length > 0 ) 
                {
				  smtpServer.SmtpAuthToken=new SmtpAuthToken(smtpServerUser, smtpServerPwd);
				}
                emailMessage.Send(smtpServer);                                  
			}
			vcontext.Put( "SENTTOEMAIL", email );
		}
		
		public void ProcessLogin( HttpContext context )
		{
            string code = context.Request[ "code" ] + String.Empty;
            string rcpt = context.Application[ "AuthCode:" + code ] + String.Empty;

            if ( rcpt.Length > 0 )
            {
                    FormsAuthentication.RedirectFromLoginPage( rcpt, true );
            }
		}		
		
		public EmailAuthLoginProcessor()
		{
		}
	}
}
