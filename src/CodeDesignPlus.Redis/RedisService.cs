using CodeDesignPlus.Redis.Option;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

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
        /// Represents a collection of System.Security.Cryptography.X509Certificates.X509Certificate2 objects
        /// </summary>
        private readonly X509Certificate2Collection collection = new();
        /// <summary>
        /// Represents the abstract multiplexer API
        /// </summary>
        private IConnectionMultiplexer connection;
        /// <summary>
        /// Describes functionality that is common to both standalone redis servers and redis clusters
        /// </summary>
        public IDatabaseAsync Database { get; private set; }
        /// <summary>
        /// A redis connection used as the subscriber in a pub/sub scenario
        /// </summary>
        public ISubscriber Subscriber { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisService"/>
        /// </summary>
        /// <param name="options">Options for the Redis service </param>
        /// <param name="logger">A generic interface for logging</param>
        public RedisService(IOptions<RedisOptions> options, ILogger<RedisService> logger)
        {
            this.options = options.Value;
            this.logger = logger;

            this.Initialize();
        }

        /// <summary>
        /// Start the connection with the redis server
        /// </summary>
        private void Initialize()
        {
            var configuration = this.options.CreateConfiguration();

            if(configuration.Ssl)
            {
                configuration.CertificateValidation += Configuration_CertificateValidation;
                configuration.CertificateSelection += Configuration_CertificateSelection;
            }

            this.connection = ConnectionMultiplexer.Connect(configuration);

            if(this.connection.IsConnected)
            {
                this.RegisterEvents();

                this.Subscriber = this.connection.GetSubscriber();

                this.Database = this.connection.GetDatabase((int)configuration.DefaultDatabase);
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
        private X509Certificate Configuration_CertificateSelection(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            this.collection.Import(options.Certificate, options.PasswordCertificate);

            return new X509Certificate(options.Certificate, options.PasswordCertificate);
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
            if(sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
            {
                var root = chain.ChainElements[^1].Certificate;

                return this.collection.Contains(root);
            }

            return sslPolicyErrors == SslPolicyErrors.None;
        }

        /// <summary>
        /// Register event handlers 
        /// </summary>
        private void RegisterEvents()
        {
            this.connection.ConfigurationChanged += this.ConfigurationChanged;
            this.connection.ConfigurationChangedBroadcast += this.ConfigurationChangedBroadcast;
            this.connection.ConnectionFailed += this.ConnectionFailed;
            this.connection.ConnectionRestored += this.ConnectionRestored;
            this.connection.ErrorMessage += this.ErrorMessage;
            this.connection.HashSlotMoved += this.HashSlotMoved;
            this.connection.InternalError += this.InternalError;
        }

        /// <summary>
        /// Raised whenever an internal error occurs (this is primarily for debugging)
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">Event information</param>
        private void InternalError(object sender, InternalErrorEventArgs args)
        {
            var endPoint = (DnsEndPoint)args.EndPoint;

            var data = new
            {
                args.ConnectionType,
                EndPoint = endPoint.ToString(),
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
            var oldEndPoint = (DnsEndPoint)args.OldEndPoint;
            var newEndPoint = (DnsEndPoint)args.NewEndPoint;

            var data = new
            {
                args.HashSlot,
                OldEndPoint = oldEndPoint.ToString(),
                NewEndPoint = newEndPoint.ToString()
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
            var endPoint = (DnsEndPoint)args.EndPoint;

            var data = new
            {
                EndPoint = endPoint.ToString(),
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
            var endPoint = (DnsEndPoint)args.EndPoint;

            var data = new
            {
                args.ConnectionType,
                EndPoint = endPoint.ToString(),
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
            var endPoint = (DnsEndPoint)args.EndPoint;

            var data = new
            {
                args.ConnectionType,
                EndPoint = endPoint.ToString(),
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
            var endPoint = (DnsEndPoint)args.EndPoint;

            var data = new
            {
                EndPoint = endPoint.ToString(),
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
            var endPoint = (DnsEndPoint)args.EndPoint;

            var data = new
            {
                EndPoint = endPoint.ToString(),
            };

            this.logger.LogInformation("Configuration Changed - Data: {0}", JsonSerializer.Serialize(data, this.jsonSerializerOptions));
        }
    }
}
