namespace DevToolbox.Tools.HttpStatusCodes
{
    /// <summary>
    /// Static reference data for the "HTTP Status Codes" tool: every status code registered by
    /// IANA's HTTP Status Code Registry, with its reason phrase and a short one-line meaning.
    /// </summary>
    public static class HttpStatusCodeData
    {
        public static readonly string[][] Rows =
        {
            new[] { "100", "Continue", "Informational - the initial part of the request has been received; client should continue." },
            new[] { "101", "Switching Protocols", "Informational - server is switching protocols as requested (e.g. to WebSocket)." },
            new[] { "102", "Processing", "Informational - server has received and is processing the request (WebDAV), no response yet." },
            new[] { "103", "Early Hints", "Informational - lets the client preload resources while the server prepares a final response." },

            new[] { "200", "OK", "Success - the request succeeded; meaning of the response body depends on the method." },
            new[] { "201", "Created", "Success - the request succeeded and a new resource was created." },
            new[] { "202", "Accepted", "Success - the request was accepted for processing, but processing isn't complete." },
            new[] { "203", "Non-Authoritative Information", "Success - returned metadata differs from what the origin server would return." },
            new[] { "204", "No Content", "Success - there is no content to send; the client should keep its current view." },
            new[] { "205", "Reset Content", "Success - client should reset the document view that sent the request." },
            new[] { "206", "Partial Content", "Success - delivering only part of the resource, per a Range header." },
            new[] { "207", "Multi-Status", "Success - a WebDAV response carrying multiple independent status codes." },
            new[] { "208", "Already Reported", "Success - WebDAV binding members already enumerated, not re-listed." },
            new[] { "226", "IM Used", "Success - server fulfilled a GET and the response is a representation of instance manipulations applied." },

            new[] { "300", "Multiple Choices", "Redirection - more than one possible response; user/agent must choose one." },
            new[] { "301", "Moved Permanently", "Redirection - the resource has permanently moved to a new URL." },
            new[] { "302", "Found", "Redirection - the resource temporarily resides at a different URL." },
            new[] { "303", "See Other", "Redirection - fetch the response from a different URL using GET." },
            new[] { "304", "Not Modified", "Redirection - cached version is still valid, no need to retransmit." },
            new[] { "305", "Use Proxy", "Redirection - deprecated; the requested resource must be accessed through a proxy." },
            new[] { "307", "Temporary Redirect", "Redirection - like 302, but the method/body must not change on retry." },
            new[] { "308", "Permanent Redirect", "Redirection - like 301, but the method/body must not change on retry." },

            new[] { "400", "Bad Request", "Client Error - the server cannot process the request due to a client-side error." },
            new[] { "401", "Unauthorized", "Client Error - authentication is required and has failed or not been provided." },
            new[] { "402", "Payment Required", "Client Error - reserved for future use (originally for digital payment schemes)." },
            new[] { "403", "Forbidden", "Client Error - the client does not have access rights to the content." },
            new[] { "404", "Not Found", "Client Error - the server can't find the requested resource." },
            new[] { "405", "Method Not Allowed", "Client Error - the request method is not supported for this resource." },
            new[] { "406", "Not Acceptable", "Client Error - no content matching the criteria in the Accept headers." },
            new[] { "407", "Proxy Authentication Required", "Client Error - the client must authenticate with a proxy first." },
            new[] { "408", "Request Timeout", "Client Error - the server timed out waiting for the request." },
            new[] { "409", "Conflict", "Client Error - the request conflicts with the current state of the resource." },
            new[] { "410", "Gone", "Client Error - the resource is no longer available and won't be again." },
            new[] { "411", "Length Required", "Client Error - the request did not specify the length of its content." },
            new[] { "412", "Precondition Failed", "Client Error - a precondition in the request headers was not met." },
            new[] { "413", "Payload Too Large", "Client Error - the request body is larger than the server is willing to process." },
            new[] { "414", "URI Too Long", "Client Error - the requested URI is longer than the server is willing to interpret." },
            new[] { "415", "Unsupported Media Type", "Client Error - the media format of the requested data is not supported." },
            new[] { "416", "Range Not Satisfiable", "Client Error - the range specified by the Range header cannot be fulfilled." },
            new[] { "417", "Expectation Failed", "Client Error - the expectation in the Expect header could not be met." },
            new[] { "418", "I'm a Teapot", "Client Error - an April Fools' joke from RFC 2324; some servers use it deliberately." },
            new[] { "421", "Misdirected Request", "Client Error - the request was directed at a server unable to produce a response." },
            new[] { "422", "Unprocessable Entity", "Client Error - well-formed request, but semantically incorrect (common with validation)." },
            new[] { "423", "Locked", "Client Error - the resource being accessed is locked (WebDAV)." },
            new[] { "424", "Failed Dependency", "Client Error - the request failed because a prior request failed (WebDAV)." },
            new[] { "425", "Too Early", "Client Error - server is unwilling to risk processing a request that might be replayed." },
            new[] { "426", "Upgrade Required", "Client Error - the server refuses this protocol version; client should upgrade." },
            new[] { "428", "Precondition Required", "Client Error - the origin server requires the request to be conditional." },
            new[] { "429", "Too Many Requests", "Client Error - the client has sent too many requests in a given time (rate limiting)." },
            new[] { "431", "Request Header Fields Too Large", "Client Error - header fields are too large for the server to process." },
            new[] { "451", "Unavailable For Legal Reasons", "Client Error - the resource is unavailable due to a legal demand." },

            new[] { "500", "Internal Server Error", "Server Error - a generic, unexpected condition was encountered." },
            new[] { "501", "Not Implemented", "Server Error - the server doesn't support the functionality required." },
            new[] { "502", "Bad Gateway", "Server Error - the server, acting as a gateway, got an invalid response upstream." },
            new[] { "503", "Service Unavailable", "Server Error - the server isn't ready to handle the request (overload/maintenance)." },
            new[] { "504", "Gateway Timeout", "Server Error - the server, acting as a gateway, didn't get a timely response upstream." },
            new[] { "505", "HTTP Version Not Supported", "Server Error - the HTTP version used in the request isn't supported." },
            new[] { "506", "Variant Also Negotiates", "Server Error - a server misconfiguration in transparent content negotiation." },
            new[] { "507", "Insufficient Storage", "Server Error - the server is unable to store the representation (WebDAV)." },
            new[] { "508", "Loop Detected", "Server Error - the server detected an infinite loop while processing (WebDAV)." },
            new[] { "510", "Not Extended", "Server Error - further extensions to the request are required for it to be fulfilled." },
            new[] { "511", "Network Authentication Required", "Server Error - the client needs to authenticate to gain network access." },
        };
    }
}
