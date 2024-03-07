using System.Collections.Generic;

namespace honooru.Services.Queues {

    public interface IProcessQueue {

        List<long> GetProcessTime();

        int Count();

        long Processed();

    }
}
