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
        /// Initializes a new instance of the <see cref="RedisService"/>
        /// </summary>
        /// <param name="options">Options for the Redis service </param>
        /// <param name="logger">A generic interface for logging</param>
        public RedisService(IOptions<RedisOptions> options, ILogger<RedisService> logger)
        {
            this.options = options.Value;
            this.logger = logger;
        }

        /// <summary>
        /// Register event handlers 
        /// </summary>
        /// <param name="connection">Represents an inter-related group of connections to redis servers</param>
        private void RegisterEvents(IConnectionMultiplexer connection)
        {
            connection.ConfigurationChanged += ConfigurationChanged;
            connection.ConfigurationChangedBroadcast += ConfigurationChangedBroadcast;
            connection.ConnectionFailed += ConnectionFailed;
            connection.ConnectionRestored += ConnectionRestored;
            connection.ErrorMessage += ErrorMessage;
            connection.HashSlotMoved += HashSlotMoved;
            connection.InternalError += InternalError;
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
