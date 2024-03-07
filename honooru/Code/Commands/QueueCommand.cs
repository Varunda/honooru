using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using honooru.Commands;
using honooru.Models.Queues;
using honooru.Services.Queues;

namespace honooru.Code.Commands {

    [Command]
    public class QueueCommand {

        private readonly ILogger<QueueCommand> _Logger;

        public QueueCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<QueueCommand>>();
        }

    }
}
