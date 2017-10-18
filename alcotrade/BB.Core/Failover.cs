using System;
using System.Collections.Generic;
using System.Threading;
using BB.Core.Log;

namespace BB.Core
{
    public static class Failover
    {
        private static readonly object _syncRoot = new object();
        static readonly List<Type> _dontLogFatalOnExceptionTypes;

        static Failover()
        {
            _dontLogFatalOnExceptionTypes = new List<Type>();
        }

        public static void Execute(Action action, Action<Exception> onException = null, bool reThrowException = false,
           Action onFinally = null, bool programExitOnException = false)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            try
            {
                action();
            }
            catch (Exception e)
            {
                if (NeedToBeLogged(e))
                    Logger.Log(e);

                if (onException != null)
                {
                    try
                    {
                        onException(e);
                    }
                    catch (Exception e1)
                    {
                        Logger.Log(e1);

                        if (programExitOnException)
                            Environment.Exit(1);
                        if (reThrowException)
                            throw;
                    }
                }

                if (programExitOnException)
                    Environment.Exit(1);

                if (reThrowException)
                    throw;
            }
            finally
            {
                if (onFinally != null)
                {
                    try
                    {
                        onFinally();
                    }
                    catch (Exception e2)
                    {
                        Logger.Log(e2);

                        if (reThrowException)
                            throw;
                    }
                }
            }
        }

        public static void Execute<T>(Action<T> action, T param, Action<Exception> onException = null,
            bool reThrowException = false,
            Action onFinally = null, bool programExitOnException = false)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            try
            {
                action(param);
            }
            catch (Exception e)
            {
                if (NeedToBeLogged(e))
                    Logger.Log(e);

                if (onException != null)
                {
                    try
                    {
                        onException(e);
                    }
                    catch (Exception e1)
                    {
                        Logger.Log(e1);

                        if (programExitOnException)
                            Environment.Exit(1);
                        
                        if (reThrowException)
                        throw;
                    }
                }

                if (programExitOnException)
                    Environment.Exit(1);

                if (reThrowException)
                    throw;
            }
            finally
            {
                if (onFinally != null)
                {
                    try
                    {
                        onFinally();
                    }
                    catch (Exception e2)
                    {
                        Logger.Log(e2);
                        if (reThrowException)
                        throw;
                    }
                }
            }
        }

        public static T Execute<T>(Func<T> action, Action<Exception> onException = null, bool reThrowException = false,
            Action onFinally = null, bool programExitOnException = false)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var result = default(T);

            try
            {
                result = action();
            }
            catch (Exception e)
            {
                if (NeedToBeLogged(e))
                    Logger.Log(e);

                if (onException != null)
                {
                    try
                    {
                        onException(e);
                    }
                    catch (Exception e1)
                    {
                        Logger.Log(e1);

                        if (programExitOnException)
                            Environment.Exit(1);

                        if (reThrowException)
                        throw;
                    }
                }

                if (programExitOnException)
                    Environment.Exit(1);

                if (reThrowException)
                    throw;
            }
            finally
            {
                if (onFinally != null)
                {
                    try
                    {
                        onFinally();
                    }
                    catch (Exception e2)
                    {
                        Logger.Log(e2);
                        if (reThrowException)
                        throw;
                    }
                }
            }

            return result;
        }

        public static T Execute<T>(Func<T> action, Func<Exception, T> onException = null, bool reThrowException = false,
            Action onFinally = null, bool programExitOnException = false)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var result = default(T);

            try
            {
                result = action();
            }
            catch (Exception e)
            {
                if (NeedToBeLogged(e))
                    Logger.Log(e);

                if (onException != null)
                {
                    try
                    {
                        result = onException(e);
                    }
                    catch (Exception e1)
                    {
                        Logger.Log(e1);

                        if (programExitOnException)
                            Environment.Exit(1);

                        if (reThrowException)
                        throw;
                    }
                }

                if (programExitOnException)
                    Environment.Exit(1);

                if (reThrowException)
                    throw;
            }
            finally
            {
                if (onFinally != null)
                {
                    try
                    {
                        onFinally();
                    }
                    catch (Exception e2)
                    {
                        Logger.Log(e2);
                        if (reThrowException)
                        throw;
                    }
                }
            }

            return result;
        }

        private static bool NeedToBeLogged(Exception e)
        {
            lock (_syncRoot)
            {
                return !_dontLogFatalOnExceptionTypes.Contains(e.GetType());
            }
        }

        public static void DontLogFatalOn<T>() where T:Exception
        {
            var type = typeof (T);
            lock (_syncRoot)
            {
                if (!_dontLogFatalOnExceptionTypes.Contains(type))
                    _dontLogFatalOnExceptionTypes.Add(type);
            }
        }

        public static void ExecuteAlways(Action action, int delayInMilliseconds, Action<Exception> onException = null,
            CancellationToken? cancellationToken = null, Action onStart = null, Action onStop = null)
        {
            try
            {
                Logger.Info("Старт потока #{0}-{2} с cancellationToken. {1}", Thread.CurrentThread.ManagedThreadId, cancellationToken.GetHashCode(), Thread.CurrentThread.Name);
                if (onStart != null) onStart();
                
                if (action == null)
                    throw new ArgumentNullException("action");

                Execute(() => CheckDelayIsMoreThanZero(delayInMilliseconds), reThrowException: true);

                while (true)
                {
                    if (cancellationToken.HasValue && cancellationToken.Value.IsCancellationRequested)
                    {
                        Logger.Info("Выполнение потока #{0}-{2} прервано по cancellationToken. {1}", Thread.CurrentThread.ManagedThreadId, cancellationToken.GetHashCode(), Thread.CurrentThread.Name);
                        return;
                    }

                    Execute(action, onException);

                    Execute(() => Thread.Sleep(delayInMilliseconds), reThrowException: true);
                }
            }
            finally
            {
                if (onStop != null) onStop();
            }
            
        }

        private static void CheckDelayIsMoreThanZero(int delayInMilliseconds)
        {
            if (delayInMilliseconds < 1000)
                throw new InvalidOperationException("ExecuteAlways delayInMilliseconds < 1000");

        }

        public static void ExecuteInTask(Action action, int delayInMilliseconds, CancellationToken cancellation,
            Action<Exception> onException = null, Action onStart = null, Action onStop = null)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            Execute(() => CheckDelayIsMoreThanZero(delayInMilliseconds), reThrowException: true);

            var thread = new Thread(() => ExecuteAlways(action, delayInMilliseconds, onException, cancellation, onStart, onStop));
            thread.Name = Guid.NewGuid().ToString();
            thread.Start();
        }

        
    }
}
