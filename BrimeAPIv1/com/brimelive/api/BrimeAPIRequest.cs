﻿#nullable enable

using System.Collections.Generic;
using BrimeAPI.com.brimelive.api.errors;

namespace BrimeAPI.com.brimelive.api {
    /// <summary>Defines API interface being used.</summary>
    public enum BrimeAPIEndpoint {
        /// <summary>Main Production API - current public release.</summary>
        PRODUCTION, 
        /// <summary>Main Testing API - next scheduled public release.</summary>
        STAGING, 
        /// <summary>Sandbox Testing API</summary>
        SANDBOX
    }

    /// <summary>
    /// Used to specify the request mode - default is normally GET.
    /// </summary>
    public enum BrimeRequestMode {
        /// <summary>
        /// Request is expected to return API response data, default request mode
        /// </summary>
        GET,
        
        /// <summary>
        /// Request is expected to trigger on server, normally no response data. (ie Used when using API to create clip on channel.)
        /// </summary>
        POST
    }

    /// <summary>
    /// Base class used to define a specific API request. This class contains the normal structures required to implement the request
    /// to the platform (ie Client-ID, API base endpoint, etc) and will be responsible for managing rate-limiting requests. (TODO)
    /// </summary>
    /// <typeparam name="ResponseType">Defines the detail class used for the response to this request</typeparam>
    public abstract class BrimeAPIRequest<ResponseType> {
        /// <summary>Class Logger instance</summary>
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Specifies whether this request requires special access on the Client-ID in order to complete successfully. Currently this is required
        /// for User-details requests. 
        /// </summary>
        public bool RequiresSpecialAccess { get; private set; } = false;



        /// <summary>
        /// Create a custom object from the request. 
        /// </summary>
        /// <example>Normal implementation: <code>
        /// BrimeAPIResponse response = doRequest();    // Process request and generate response. <br/>
        /// BrimeAPIError.ThrowException(response);     // Trigger any exceptions generated by the request.<br/>
        /// return new ResponseType(response.Data);     // Construct the custom object for this type of request.<br/>
        /// </code></example>
        /// <returns>Custom object based on the type of information requested.</returns>
        /// <exception cref="BrimeAPIInternalError">Internal server error.</exception>
        /// <exception cref="BrimeAPINotImplemented">This endpoint is not implemented.</exception>
        /// <exception cref="BrimeAPIMissingParameter">Request is missing a required parameter</exception>
        /// <exception cref="BrimeAPIInvalidClientID">Request included an invalid Client-ID</exception>
        /// <exception cref="BrimeAPIMissingScope">Request ID was missing the required scope for this endpoint</exception>
        /// <exception cref="BrimeAPIInvalidChannel">Request referenced an invalid channel</exception>
        /// <exception cref="BrimeAPIRateLimitExceeded">Too many requests have been issued recently</exception>
        /// <exception cref="BrimeAPIException">An error not currently recognised by this API implementation was returned</exception>
        /// 
        public abstract ResponseType getResponse();

        /// <summary>
        /// Request format for this particular request. This is set by the specific implementing class to define the path to the specific
        /// endpoint, and will include placeholders to be replaced by <see cref="RequestParameters"/> using <see cref="System.String.Format(string, object[])"/>
        /// </summary>
        protected string RequestFormat { get; set; }

        /// <summary>
        /// Defines the delegate structure used to specify the string parameters. Delegate is used to ensure that the parameters are retrieved
        /// at the moment of the request, allowing them to be changed between creation of the request and the actual call, or between multiple
        /// invokations of a particular call. Particularly used for requests which allow paging through results - same request can just be 
        /// updated to skip the appropriate number of items and be re-triggered without other modification.
        /// </summary>
        /// <returns>an array of string entries to be used to fill the placeholders in <see cref="RequestFormat"/></returns>
        protected delegate string[] GetRequestParameters();

        /// <summary>
        /// Parameters to be included in the API request. This is set by the specific implementing class as a function which will select the
        /// correct set of parameters when the request is triggered. Default implementation is for requests which require no parameters.
        /// </summary>
        protected GetRequestParameters RequestParameters { get; set; } = (() => { return new string[0]; });

        /// <summary>
        /// Defines the delegate structure used to specify query parameters. 
        /// </summary>
        /// <returns>Array of query parameters to be added to the request</returns>
        protected delegate System.Collections.Generic.KeyValuePair<string, string>[] GetQueryParameters();

        /// <summary>
        /// Query parameters to be included in the API request. 
        /// </summary>
        protected GetQueryParameters QueryParameters { get; set; } = (() => { return new System.Collections.Generic.KeyValuePair<string, string>[0]; });

        /// <summary>
        /// Specify the mode to use for this API request. Default is GET, but can also be set to POST request.
        /// </summary>
        protected BrimeRequestMode RequestMode { get; set; } = BrimeRequestMode.GET;

        /// <summary>
        /// Defines the delegate structure used to specify a post body for a request.
        /// </summary>
        /// <returns>string to be used for the post body in a POST request</returns>
        protected delegate string GetPostBody();

        /// <summary>
        /// Used only with POST requests to specify the body of the request to send. Default just returns an empty string.
        /// </summary>
        protected GetPostBody PostBody { get; set; } = (() => { return ""; });

        /// <summary>
        /// Create a new API request. Used directly when needing to include <c>requiresSpecialAccess=true</c>.
        /// </summary>
        /// <param name="requestFormat">used to set <c>RequestFormat</c></param>
        /// <param name="requiresSpecialAccess">true if this request requires special API access, false for most requests</param>
        public BrimeAPIRequest(string requestFormat, bool requiresSpecialAccess) {
            this.RequestFormat = requestFormat;
            this.RequiresSpecialAccess = requiresSpecialAccess;
        }

        /// <summary>
        /// Normal constructor, will call other constructor defaulting <c>requiresSpecialAccess=false</c>
        /// </summary>
        /// <param name="requestFormat">used to set <c>RequestFormat</c></param>
        public BrimeAPIRequest(string requestFormat) : this(requestFormat, false) {}

        /// <summary>
        /// Helper method to retrieve the correct API endpoint base.
        /// </summary>
        /// <returns>base website address for the currently selected API Endpoint</returns>
        protected string getAPIEndpoint() {
            return BrimeAPI.APIEndpoint switch {
                BrimeAPIEndpoint.PRODUCTION => "https://api.brimelive.com",
                BrimeAPIEndpoint.STAGING => "https://api-staging.brimelive.com/v1",
                _ => "https://api-sandbox.brimelive.com",
            };
        }

        /// <summary>
        /// Helper method to construct the full API URL. Takes <c>getAPIEndpoint</c> and appends <c>RequestFormat</c>, correctly
        /// replacing placeholders with <c>RequestParameters</c>.
        /// </summary>
        /// <returns>URL string for the full address specified in this API request</returns>
        protected string composeRequest() {
            string _result = getAPIEndpoint();
            _result += string.Format(RequestFormat, RequestParameters.Invoke());
            bool hasQuery = _result.Contains("?");
            foreach (System.Collections.Generic.KeyValuePair<string, string> param in QueryParameters.Invoke()) {
                _result += (hasQuery ? "&" : "?");
                _result += param.Key + "=" + param.Value;
                hasQuery = true;
            }
            // TODO: Remove this when using Client-ID header instead
            _result += (hasQuery ? "&" : "?") + "client_id=" + BrimeAPI.ClientID;
            return _result;
        }

        /// <summary>
        /// Helper method to trigger this API request. This central method is used to actually perform the request to the 
        /// endpoint, and process the response. This method is centralized to allow easy activation of rate-limiting of 
        /// the service. Note this method should not trigger any exceptions due to errors in the response data.
        /// </summary>
        /// <returns>API response created as a result of this request</returns>
        protected BrimeAPIResponse doRequest() {
            string request = composeRequest();

            // May pass off to central request handler for async processing
            Logger.Debug(() => { return "REQUEST: " + request; });
            if (RequiresSpecialAccess) Logger.Warn("Request requires SPECIAL ACCESS enabled for the Client-ID.");

            // Call the Rate-Limited request handler to ensure no more than 5 QPS are being made.
            // TODO: Update to send Client-ID as header
            return RateLimitedRequestHandler.Instance.doRequest(request, RequestMode, 
                // Headers to add to request
                new KeyValuePair<string,string>[] { 
                    // TODO: Add Client-ID as header: new KeyValuePair<string, string>("Client_Id", BrimeAPI.ClientID) 
                }, 
                PostBody.Invoke());
            /*
            // TODO: Include delay to wait if too many requests have been sent.
            // Following section may be moved to a central static method which limits how many requests can be sent
            // in a given period of time. (Required 5 QPS by API specification.)
            WebRequest req = WebRequest.Create(request);
            if (RequestMode == BrimeRequestMode.POST) {
                byte[] data = Encoding.UTF8.GetBytes(PostBody.Invoke());
                req.Method = "POST";
                req.ContentType = "application/json";
                req.ContentLength = data.Length;

                using (var stream = req.GetRequestStream()) {
                    stream.Write(data, 0, data.Length);
                }
            }
            WebResponse response = req.GetResponse();
            string json = new StreamReader(response.GetResponseStream()).ReadToEnd();
            Logger.Debug(() => { return "RESPONSE: " + response; });
            return new BrimeAPIResponse(json);
            */
        }
    }
}
