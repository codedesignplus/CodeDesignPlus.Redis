using CodeDesignPlus.Redis.Attributes;
using StackExchange.Redis;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;

namespace CodeDesignPlus.Redis.Option
{
    /// <summary>
    /// Configuration options for the Redis service 
    /// </summary>
    public class RedisOptions
    {
        /// <summary>
        /// Section Name
        /// </summary>
        public const string Section = "Redis";
        /// <summary>
        /// Specifies that DNS resolution should be explicit and eager, rather than implicit
        /// </summary>
        public bool ResolveDns { get; set; } = false;
        /// <summary>
        /// Password for the redis server
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// User for the redis server (for use with ACLs on redis 6 and above)
        /// </summary>
        public string User { get; set; }
        /// <summary>
        /// Use ThreadPriority.AboveNormal for SocketManager reader and writer threads (true by default). If false, ThreadPriority.Normal will be used.
        /// </summary>
        public bool HighPrioritySocketThreads { get; set; } = true;
        /// <summary>
        /// The endpoints defined for this configuration
        /// </summary>
        [EndpointIsValid]
        public List<string> EndPoints { get; } = new List<string>();
        /// <summary>
        /// Specifies the default database to be used when calling ConnectionMultiplexer.GetDatabase() without any parameters
        /// </summary>
        [Range(0, int.MaxValue)]
        public int DefaultDatabase { get; set; } = 0;
        /// <summary>
        /// Specifies the time in milliseconds that should be allowed for connection (defaults to 5 seconds unless SyncTimeout is higher)
        /// </summary>
        public int ConnectTimeout { get; set; } = 5000;
        /// <summary>
        /// The number of times to repeat the initial connect cycle if no servers respond promptly
        /// </summary>
        public int ConnectRetry { get; set; } = 3;
        /// <summary>
        /// The client name to use for all connections (GetAppName())
        /// </summary>
        public string ClientName { get; set; }
        /// <summary>
        /// A Boolean value that specifies whether the certificate revocation list is checked during authentication.
        /// </summary>
        public bool CheckCertificateRevocation { get; set; } = true;
        /// <summary>
        /// Optional channel prefix for all pub/sub operations
        /// </summary>
        public string ChannelPrefix { get; set; }
        /// <summary>
        /// Time (ms) to allow for asynchronous operations (Default SyncTimeout)
        /// </summary>
        public int AsyncTimeout { get; set; } = 5000;
        /// <summary>
        /// Indicates whether admin operations should be allowed
        /// </summary>
        public bool AllowAdmin { get; set; } = false;
        /// <summary>
        /// Gets or sets whether connect/configuration timeouts should be explicitly notified via a TimeoutException
        /// </summary>
        public bool AbortOnConnectFail { get; set; } = true;
        /// <summary>
        /// Indicates whether the connection should be encrypted
        /// </summary>
        public bool Ssl { get; set; } = true;
        /// <summary>
        /// The target-host to use when validating SSL certificate; setting a value here enables SSL mode
        /// </summary>
        public string SslHost { get; set; }
        /// <summary>
        /// Specifies the time in milliseconds that the system should allow for synchronous operations (defaults to 5 seconds)
        /// </summary>
        public int SyncTimeout { get; set; } = 5000;
        /// <summary>
        /// Check configuration every n seconds (every minute by default)
        /// </summary>
        public int ConfigCheckSeconds { get; set; } = 60;
        /// <summary>
        /// The service name used to resolve a service via sentinel.
        /// </summary>
        public string ServiceName { get; set; }
        /// <summary>
        /// File Pfx
        /// </summary>
        public string Certificate { get; set; }
        /// <summary>
        /// Password Certificate
        /// </summary>
        public string PasswordCertificate { get; set; }

        /// <summary>
        /// Create a new instance of <see cref="ConfigurationOptions"/>
        /// </summary>
        /// <returns>The options relevant to a set of redis connections</returns>
        public ConfigurationOptions CreateConfiguration()
        {
            var configuration = new ConfigurationOptions()
            {
                ResolveDns = this.ResolveDns,
                Password = this.Password,
                User = this.User,
                HighPrioritySocketThreads = this.HighPrioritySocketThreads,
                DefaultDatabase = this.DefaultDatabase,
                ConnectTimeout = this.ConnectTimeout,
                ConnectRetry = this.ConnectRetry,
                ClientName = this.ClientName,
                CheckCertificateRevocation = this.CheckCertificateRevocation,
                ChannelPrefix = this.ChannelPrefix,
                AsyncTimeout = this.AsyncTimeout,
                AllowAdmin = this.AllowAdmin,
                AbortOnConnectFail = this.AbortOnConnectFail,
                Ssl = this.Ssl,
                SslHost = this.SslHost,
                SyncTimeout = this.SyncTimeout,
                ConfigCheckSeconds = this.ConfigCheckSeconds,
                ServiceName = this.ServiceName,
            };

            this.EndPoints.ForEach(x => configuration.EndPoints.Add(x));

            return configuration;
        }
    }
}
