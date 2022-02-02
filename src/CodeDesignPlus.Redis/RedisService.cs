using CodeDesignPlus.Redis.Option;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace CodeDesignPlus.Redis
{
    /// <summary>
    /// The default IServiceProvider. 
    /// </summary>
    public class RedisService : IRedisService
    {
        /// <summary>
        /// Options to control serialization behavior.
        /// </summary>
        private readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };
        /// <summary>
        /// A generic interface for logging
        /// </summary>
        private readonly ILogger<RedisService> logger;
        /// <summary>
        /// Options for the Redis service 
        /// </summary>
        private readonly RedisOptions options;
        /// <summary>
        /// Represents the abstract multiplexer API
        /// </summary>
        public IConnectionMultiplexer Connection { get; private set; }
        /// <summary>
        /// Describes functionality that is common to both standalone redis servers and redis clusters
        /// </summary>
        public IDatabaseAsync Database { get; private set; }
        /// <summary>
        /// A redis connection used as the subscriber in a pub/sub scenario
        /// </summary>
        public ISubscriber Subscriber { get; private set; }
        /// <summary>
        /// Indicates whether any servers are connected
        /// </summary>
        public bool IsConnected { get => this.Connection.IsConnected; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisService"/>
        /// </summary>
        /// <param name="options">Options for the Redis service </param>
        /// <param name="logger">A generic interface for logging</param>
        /// <exception cref="ArgumentNullException">options is null</exception>
        /// <exception cref="ArgumentNullException">logger is null</exception>
        public RedisService(IOptions<RedisOptions> options, ILogger<RedisService> logger)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            this.options = options.Value;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.Initialize();
        }

        /// <summary>
        /// Start the connection with the redis server
        /// </summary>
        private void Initialize()
        {
            var configuration = this.options.CreateConfiguration();

            if (configuration.Ssl)
            {
                configuration.CertificateSelection += Configuration_CertificateSelection;
                configuration.CertificateValidation += Configuration_CertificateValidation;
            }

            this.Connection = ConnectionMultiplexer.Connect(configuration);

            if (this.Connection.IsConnected)
            {
                this.RegisterEvents();

                this.Subscriber = this.Connection.GetSubscriber();

                this.Database = this.Connection.GetDatabase((int)configuration.DefaultDatabase);
            }
        }

        /// <summary>
        /// A LocalCertificateSelectionCallback delegate responsible for selecting the certificate
        /// used for authentication; note that this cannot be specified in the configuration-string.
        /// </summary>
        /// <param name="sender">An object that contains state information for this validation.</param>
        /// <param name="targetHost">The host server specified by the client.</param>
        /// <param name="localCertificates">An System.Security.Cryptography.X509Certificates.X509CertificateCollection containing local certificates.</param>
        /// <param name="remoteCertificate">The certificate used to authenticate the remote party.</param>
        /// <param name="acceptableIssuers"> A System.String array of certificate issuers acceptable to the remote party.</param>
        /// <returns>An System.Security.Cryptography.X509Certificates.X509Certificate used for establishing an SSL connection.</returns>
        private X509Certificate2 Configuration_CertificateSelection(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            if (!string.IsNullOrEmpty(this.options.PasswordCertificate))
                return new X509Certificate2(this.options.Certificate, this.options.PasswordCertificate);
            else
                return new X509Certificate2(this.options.Certificate);
        }

        /// <summary>
        /// A RemoteCertificateValidationCallback delegate responsible for validating the
        /// certificate supplied by the remote party; note that this cannot be specified
        /// in the configuration-string.
        /// </summary>
        /// <param name="sender">An object that contains state information for this validation.</param>
        /// <param name="certificate">The certificate used to authenticate the remote party.</param>
        /// <param name="chain">The chain of certificate authorities associated with the remote certificate.</param>
        /// <param name="sslPolicyErrors">One or more errors associated with the remote certificate.</param>
        /// <returns>A System.Boolean value that determines whether the specified certificate is accepted for authentication.</returns>
        private bool Configuration_CertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
            {
                var root = chain.ChainElements[^1].Certificate;

                var collection = new X509Certificate2Collection();

                collection.Import(this.options.Certificate, this.options.PasswordCertificate);

                return collection.Contains(root);
            }

            return sslPolicyErrors == SslPolicyErrors.None;
        }

        /// <summary>
        /// Register event handlers 
        /// </summary>
        private void RegisterEvents()
        {
            this.Connection.ConfigurationChanged += this.ConfigurationChanged;
            this.Connection.ConfigurationChangedBroadcast += this.ConfigurationChangedBroadcast;
            this.Connection.ConnectionFailed += this.ConnectionFailed;
            this.Connection.ConnectionRestored += this.ConnectionRestored;
            this.Connection.ErrorMessage += this.ErrorMessage;
            this.Connection.HashSlotMoved += this.HashSlotMoved;
            this.Connection.InternalError += this.InternalError;
        }

        /// <summary>
        /// Raised whenever an internal error occurs (this is primarily for debugging)
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">Event information</param>
        private void InternalError(object sender, InternalErrorEventArgs args)
        {
            var data = new
            {
                args.ConnectionType,
                EndPoint = args.EndPoint.ToString(),
                args.Origin
            };

            this.logger.LogCritical(args.Exception, "Internal Error - Data: {0}", JsonSerializer.Serialize(data, this.jsonSerializerOptions));
        }

        /// <summary>
        /// Raised when a hash-slot has been relocated
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">Event information</param>
        private void HashSlotMoved(object sender, HashSlotMovedEventArgs args)
        {
            var data = new
            {
                args.HashSlot,
                OldEndPoint = args.OldEndPoint.ToString(),
                NewEndPoint = args.NewEndPoint.ToString()
            };

            this.logger.LogWarning("Hash Slot Moved - Data: {0}", JsonSerializer.Serialize(data, this.jsonSerializerOptions));
        }

        /// <summary>
        /// A server replied with an error message;
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">Event information</param>
        private void ErrorMessage(object sender, RedisErrorEventArgs args)
        {
            var data = new
            {
                EndPoint = args.EndPoint.ToString(),
                args.Message
            };

            this.logger.LogError("Error Message - Data: {0}", JsonSerializer.Serialize(data, this.jsonSerializerOptions));
        }

        /// <summary>
        /// Raised whenever a physical connection is established
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">Event information</param>
        private void ConnectionRestored(object sender, ConnectionFailedEventArgs args)
        {
            var data = new
            {
                args.ConnectionType,
                EndPoint = args.EndPoint.ToString(),
                args.FailureType,
                physicalNameConnection = args.ToString()
            };

            this.logger.LogInformation(args.Exception, "Connection Restored - Data: {0}", JsonSerializer.Serialize(data, this.jsonSerializerOptions));
        }

        /// <summary>
        /// Raised whenever a physical connection fails
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">Event information</param>
        private void ConnectionFailed(object sender, ConnectionFailedEventArgs args)
        {
            var data = new
            {
                args.ConnectionType,
                EndPoint = args.EndPoint.ToString(),
                args.FailureType,
                physicalNameConnection = args.ToString()
            };

            this.logger.LogInformation(args.Exception, "Connection Failed - Data: {0}", JsonSerializer.Serialize(data, this.jsonSerializerOptions));
        }

        /// <summary>
        /// Raised when nodes are explicitly requested to reconfigure via broadcast; this usually means master/replica changes
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">Event information</param>
        private void ConfigurationChangedBroadcast(object sender, EndPointEventArgs args)
        {
            var data = new
            {
                EndPoint = args.EndPoint.ToString(),
            };

            this.logger.LogInformation("Configuration Changed Broadcast - Data: {0}", JsonSerializer.Serialize(data, this.jsonSerializerOptions));
        }

        /// <summary>
        /// Raised when configuration changes are detected
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">Event information</param>
        private void ConfigurationChanged(object sender, EndPointEventArgs args)
        {
            var data = new
            {
                EndPoint = args.EndPoint.ToString(),
            };

            this.logger.LogInformation("Configuration Changed - Data: {0}", JsonSerializer.Serialize(data, this.jsonSerializerOptions));
        }
    }
}
