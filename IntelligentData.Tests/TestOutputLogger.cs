using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    public class TestOutputLogger : ILogger, ILoggerProvider, ILoggerFactory
    {
        private readonly ITestOutputHelper _output;

        private StateWrapper _state = null;

        private class StateWrapper : IDisposable
        {
            private readonly object?          _val;
            private readonly TestOutputLogger _logger;
            private readonly StateWrapper?     _previous;

            public StateWrapper(TestOutputLogger logger, object? value)
            {
                _val        = value;
                _logger     = logger;
                _previous   = logger._state;
                logger._state = this;
            }

            public void Dispose()
            {
                if (ReferenceEquals(_logger._state, this))
                {
                    _logger._state = _previous;
                }
                else
                {
                    _logger.LogWarning($"Dispose out of order! ( {_logger._state._val} <> {_val} )");
                }
            }

            public override string ToString()
            {
                return _val?.ToString() ?? "";
            }
        }
        
        private TestOutputLogger(ITestOutputHelper output, object state)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _state  = new StateWrapper(this, state);
        }
        
        public TestOutputLogger(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }
        
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var msg = ((_state is null) ? "" : (_state + ": ")) + formatter(state, exception);
            var tag = logLevel.ToString().ToUpper() + ": ";
            foreach (var line in msg.Replace("\r\n", "\n").Split('\n'))
            {
                _output.WriteLine(tag + line);
            }
        }

        public bool IsEnabled(LogLevel logLevel) 
            => true;

        public IDisposable BeginScope<TState>(TState state) => new StateWrapper(this, state);
            
        public void    Dispose()
        {
            
        }

        public ILogger CreateLogger(string         categoryName) => new TestOutputLogger(_output, categoryName);
        
        public void    AddProvider(ILoggerProvider provider)
        {
            
        }
    }
}
