using Microsoft.Extensions.Logging;
using Moq;
using System;

namespace CodeDesignPlus.Redis.Test.Helpers.Extensions
{
    /// <summary>
    /// Methods extensions to ILooger
    /// </summary>
    /// <remarks>https://adamstorr.azurewebsites.net/blog/mocking-ilogger-with-moq</remarks>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Verify Logging
        /// </summary>
        /// <typeparam name="T">The type who's name is used for the logger category name.</typeparam>
        /// <param name="logger">ILogger</param>
        /// <param name="expectedMessage">Expected Message</param>
        /// <param name="expectedLogLevel">Expected Log Level</param>
        /// <param name="times">Times</param>
        /// <returns>Return Logger</returns>
        public static Mock<ILogger<T>> VerifyLogging<T>(this Mock<ILogger<T>> logger, string expectedMessage, LogLevel expectedLogLevel = LogLevel.Debug, Times? times = null)
        {
            times ??= Times.Once();

            Func<object, Type, bool> state = (v, t) => v.ToString().CompareTo(expectedMessage) == 0;

            logger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == expectedLogLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => state(v, t)),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), (Times)times);

            return logger;
        }
    }
}
