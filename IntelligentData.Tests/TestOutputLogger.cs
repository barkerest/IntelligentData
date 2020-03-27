using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    public class TestOutputLogger : ILogger
    {
        private readonly ITestOutputHelper _output;

        private object _state = null;
        
        private class StateWrapper : IDisposable
        {
            private TestOutputLogger _self;
            private object _prevState;
            
            public StateWrapper(TestOutputLogger self, object state)
            {
                _self = self;
                _prevState = self._state;
                self._state = state;
            }
            
            public void Dispose()
            {
                _self._state = _prevState;
            }
        }
        
        public TestOutputLogger(ITestOutputHelper output)
        {
            _output = output;
        }
        
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var msg = ((_state is null) ? "" : (_state.ToString() + "\n")) + formatter(state, exception);
            var tag = logLevel.ToString().ToUpper() + ": ";
            foreach (var line in msg.Replace("\r\n", "\n").Split('\n'))
            {
                _output.WriteLine(tag + line);
            }
        }

        public bool IsEnabled(LogLevel logLevel) 
            => true;

        public IDisposable BeginScope<TState>(TState state)
            => new StateWrapper(this, state);
    }
}
