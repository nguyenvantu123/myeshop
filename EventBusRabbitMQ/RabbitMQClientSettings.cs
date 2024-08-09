using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBusRabbitMQ
{
    public sealed class RabbitMQClientSettings
    {
        /// <summary>
        /// Gets or sets the connection string of the RabbitMQ server to connect to.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// <para>Gets or sets the maximum number of connection retry attempts.</para>
        /// <para>Default value is 5, set it to 0 to disable the retry mechanism.</para>
        /// </summary>
        public int MaxConnectRetryCount { get; set; } = 5;

        /// <summary>
        /// Gets or sets a boolean value that indicates whether the RabbitMQ health check is enabled or not.
        /// </summary>
        /// <value>
        /// The default value is <see langword="true"/>.
        /// </value>
        public bool HealthChecks { get; set; } = true;

        /// <summary>
        /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
        /// </summary>
        /// <value>
        /// The default value is <see langword="true"/>.
        /// </value>
        public bool Tracing { get; set; } = true;
    }

}
