﻿using RestSharp;
using System;
using System.Net;
using System.Threading;

namespace StopWatch
{
    internal class RequestDeniedException : Exception
    {
    }


    internal class JiraApiRequester : IJiraApiRequester
    {
        public JiraApiRequester(IRestClientFactory restClientFactory, IJiraApiRequestFactory jiraApiRequestFactory)
        {
            this.restClientFactory = restClientFactory;
            this.jiraApiRequestFactory = jiraApiRequestFactory;
        }


        public T DoAuthenticatedRequest<T>(IRestRequest request)
            where T : new()
        {
            IRestClient client = restClientFactory.Create();

            IRestResponse<T> response = client.Execute<T>(request);

            // If login session has expired, try to login, and then re-execute the original request
            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.BadRequest)
            {
                if (!ReAuthenticate())
                    throw new RequestDeniedException();

                // Jira needs a short delay afther ReAuthentication, before actual requests can be made
                Thread.Sleep(500);

                response = client.Execute<T>(request);
            }

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
                throw new RequestDeniedException();

            return response.Data;
        }


        protected bool ReAuthenticate()
        {
            IRestRequest request;

            try
            {
                request = jiraApiRequestFactory.CreateReAuthenticateRequest();
            }
            catch (AuthenticateNotYetCalledException)
            {
                return false;
            }

            var client = restClientFactory.Create(true);
            IRestResponse response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK)
                return false;

            return true;
        }


        private IRestClientFactory restClientFactory;
        private IJiraApiRequestFactory jiraApiRequestFactory;
    }
}
