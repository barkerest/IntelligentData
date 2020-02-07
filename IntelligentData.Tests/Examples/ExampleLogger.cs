using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace IntelligentData.Tests.Examples
{
    public class ExampleLogger : ILogger
    {
        private readonly ITestOutputHelper _output;

        public ExampleLogger(ITestOutputHelper output = null)
        {
            _output = output;
        }
        
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (_output is null) return;

            var msg = formatter(state, exception);
            _output.WriteLine(msg);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        private class Scope : IDisposable
        {
            public void Dispose()
            {
            }
        }
        
        public IDisposable BeginScope<TState>(TState state) => new Scope();
    }
}
