using StackExchange.Redis;

namespace CodeDesignPlus.Redis
{
    /// <summary>
    /// Manage the connection to the Redis cluster 
    /// </summary>
    public interface IRedisService
    {
        /// <summary>
        /// Represents the abstract multiplexer API
        /// </summary>
        IConnectionMultiplexer Connection { get; }
        /// <summary>
        /// Describes functionality that is common to both standalone redis servers and redis clusters
        /// </summary>
        IDatabaseAsync Database { get; }
        /// <summary>
        /// A redis connection used as the subscriber in a pub/sub scenario
        /// </summary>
        ISubscriber Subscriber { get; }
        /// <summary>
        /// Indicates whether any servers are connected
        /// </summary>
        bool IsConnected { get; }
    }
}
