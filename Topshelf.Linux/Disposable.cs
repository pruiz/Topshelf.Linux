namespace System
{
    public class Disposable : IDisposable
    {
        private bool _disposed;
        private readonly Action _disposeAction;

        public Disposable(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public Disposable(Action createAction, Action disposeAction)
        {
            createAction();
            _disposeAction = disposeAction;
        }

        public static Disposable For(Action disposeAction)
        {
            return new Disposable(disposeAction);
        }

        public static Disposable For(Action createAction, Action disposeAction)
        {
            return new Disposable(createAction, disposeAction);
        }


        public void Dispose()
        {
            if (!_disposed && _disposeAction != null)
            {
                _disposed = true;
                _disposeAction();
            }
        }
    }
}
