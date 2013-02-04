﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using CommonJobs.Infrastructure.Mvc;
using CommonJobs.Domain;
using DotNetOpenAuth.OpenId.RelyingParty;
using DotNetOpenAuth.OpenId.Extensions.AttributeExchange;

namespace CommonJobs.Mvc.UI.Controllers
{
    public class GoogleAuthenticationController : CommonJobsController
    {
        private const string GoogleOAuthEndpoint = "https://www.google.com/accounts/o8/id";
        private const string EmailSuffix = "@makingsense.com";
        public const string SessionRolesKey = "CommonJobs/Roles";

        public ActionResult Index(string returnUrl = null)
        {
            using (OpenIdRelyingParty openid = new OpenIdRelyingParty())
            {
                //Set up the callback URL
                var callbackUrl = Url.Action("LogOnCallback", "GoogleAuthentication", new { returnUrl = returnUrl }, Request.IsSecureConnection ? "https" : "http");
                Uri callbackUri = new Uri(callbackUrl);

                //Set up request object for Google Authentication
                IAuthenticationRequest request =
                    openid.CreateRequest(GoogleOAuthEndpoint,
                    DotNetOpenAuth.OpenId.Realm.AutoDetect, callbackUri);


                //Let's tell Google, what we want to have from the user:
                var fetch = new FetchRequest();
                fetch.Attributes.AddRequired(WellKnownAttributes.Contact.Email);
                request.AddExtension(fetch);

                //Redirect to Google Authentication
                request.RedirectToProvider();
            }
            return null;
        }

        public ActionResult LogOnCallback(string returnUrl)
        {
            OpenIdRelyingParty openid = new OpenIdRelyingParty();
            var response = openid.GetResponse();

            if (response == null)
            {
                throw new ApplicationException("No authentication response");
            }

            if (response.Status != AuthenticationStatus.Authenticated)
            {
                throw new ApplicationException("response status is not Authenticated");
            }

            var fetch = response.GetExtension<FetchResponse>();

            if (fetch == null)
            {
                throw new ApplicationException("No fetch response");
            }

            string email = fetch.GetAttributeValue(WellKnownAttributes.Contact.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ApplicationException("Email is empty");
            }
            

            if (!email.EndsWith(EmailSuffix))
            {
                throw new ApplicationException("Only emails ended with " + EmailSuffix + " are allowed");
            }

            var username = email.Substring(0, email.Length - EmailSuffix.Length);
            Session[SessionRolesKey] = null;
            FormsAuthentication.SetAuthCookie(username, false);
            
            var user = RavenSession.Query<User>().Where(u => u.UserName == username).FirstOrDefault();

            
            if (user != null) 
            {
                Session[SessionRolesKey] = user.Roles;
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
