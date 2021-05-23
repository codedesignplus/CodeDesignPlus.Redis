using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CodeDesignPlus.Redis.Attributes
{
    /// <summary>
    /// Check if endpoint is valid
    /// </summary>
    public class EndpointIsValid : ValidationAttribute
    {
        /// <summary>
        /// Matches valid IP addresses
        /// </summary>
        private const string ValidIpAddressRegex = @"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$";

        /// <summary>
        /// Matches valid Hostname. Valid as per (RFC 1123 <see cref="https://datatracker.ietf.org/doc/html/rfc1123"/>)
        /// </summary>
        private const string ValidHostnameRegex = @"^(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z0-9]|[A-Za-z0-9][A-Za-z0-9\-]*[A-Za-z0-9])$";

        /// <summary>
        /// Determines whether the specified value of the object is valid.
        /// </summary>
        /// <param name="data">The value of the object to validate.</param>
        /// <returns>true if the specified value is valid; otherwise, false.</returns>
        public override bool IsValid(object data)
        {
            var endpoints = (List<string>) data;

            var validIpAddressRegex = new Regex(ValidIpAddressRegex);
            var validHostnameRegex = new Regex(ValidIpAddressRegex);

            foreach (string endpoint in endpoints)
            {
                if (!validIpAddressRegex.IsMatch(endpoint) || !validHostnameRegex.IsMatch(endpoint))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
