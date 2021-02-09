using Mailjet.Client;
using Mailjet.Client.Resources;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rocky.Utility
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration; // access the information from configuration (dependency between appsettings.json and utility)
        public MailJetSettings _mailJetSettings { get; set; }
        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return Execute(email, subject, htmlMessage);
        }
        public Task Execute(string email, string subject, string body)
        {
            _mailJetSettings = _configuration.GetSection("MailJet").Get<MailJetSettings>(); // because MailJet is the name of the object in appsettings
            MailjetClient client = new MailjetClient(_mailJetSettings.ApiKey, _mailJetSettings.SecretKey);
            /*
            {
                Version = ApiVersion.V3_1;
            };
            */
            MailjetRequest request = new MailjetRequest
            {
                Resource = Send.Resource,
            }
             .Property(Send.Messages, new JArray {
     new JObject {
      {
       "From",
       new JObject {
        {"Email", "guardmkd@protonmail.com"},
        {"Name", "TEST EMAIL"}
       }
      }, {
       "To",
       new JArray {
        new JObject {
         {
          "Email",
          email // changed
         }, {
          "Name",
          "ProjectNAME" // changed
         }
        }
       }
      }, {
       "Subject",
       subject // changed
      }, {
       "HTMLPart",
        body      
       }
     }
             });
            return client.PostAsync(request); // removed MailjetResponse response = 
        }
    }
}
