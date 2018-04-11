using System;
using System.Collections.Generic;

namespace MessangerConsole
{
    internal class DisposeChain : IDisposable
    {
        private readonly IDisposable one;
        private readonly IDisposable two;
        private bool _Disposed;

        public DisposeChain(IDisposable one, IDisposable two)
        {
            this.one = one;
            this.two = two;
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        private void Dispose(bool dispose)
        {
            if (_Disposed)
            {
                return;
            }

            _Disposed = dispose;

            try
            {
                one.Dispose();
            }
            catch (Exception error)
            {
                try
                {
                    two.Dispose();
                }
                catch (AggregateException aggregateException)
                {
                    var errors = new List<Exception>(aggregateException.InnerExceptions)
                    {
                        error
                    };
                    throw new AggregateException(errors);
                }
                catch (Exception e)
                {
                    throw new AggregateException(error, e);
                }

                throw;
            }
        }
    }
}


