using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace honooru.Tests.Util {

    public class TestLogger<T> : ILogger<T> {

        private string _ClassPrefix;

        public TestLogger() {
            _ClassPrefix = typeof(T).FullName ?? typeof(T).Name;
        }

        public IDisposable BeginScope<TState>(TState state) {
            return default!;
        }

        public bool IsEnabled(LogLevel logLevel) {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            Console.WriteLine($"[{DateTime.UtcNow:u}] {logLevel}: {_ClassPrefix}> {formatter(state, exception)}");
        }

    }

}
