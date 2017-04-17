using System;
using System.Collections.Generic;
using System.Text;
#if NETSTANDARD
using System.Threading;
#else
using System.Timers;
#endif

namespace LumiSoft.Net
{
#if NETSTANDARD
    public delegate void TimerExCallback(Object state);
#else
    public delegate void TimerExCallback(Object sender, System.Timers.ElapsedEventArgs e);
#endif

    /// <summary>
    /// Simple timer implementation.
    /// </summary>
    public class TimerEx : IDisposable
    {
        private readonly Timer m_Timer;

        // defaults taken from System.Timers.Timer documentation
        private const int DEFAULT_INTERVAL = 100;
        private const bool DEFAULT_AUTORESET = true;

#if NETSTANDARD
        private static readonly TimeSpan NO_START = TimeSpan.FromMilliseconds(-1);
        private static readonly TimeSpan START_NOW = TimeSpan.Zero;

        private Double m_Interval = DEFAULT_INTERVAL;
        private Boolean m_AutoReset = DEFAULT_AUTORESET;
        private Int32 m_AutoResetRun = 0;
        private Boolean m_IsRunning = false;

        TimerCallback WrapCallback(TimerExCallback inner)
        {
            return 
                delegate (Object state) {
                    if (!m_AutoReset) {
                        if (m_AutoResetRun > 0) {
                            Stop();
                            return;
                        }
                        m_AutoResetRun++;
                    }
                    inner(state);
                };
        }
#endif

        /// <summary>
        /// Default contructor.
        /// </summary>
        public TimerEx(TimerExCallback callback) 
        {
#if NETSTANDARD
            m_Timer = new Timer(WrapCallback(callback), null, NO_START, TimeSpan.FromMilliseconds(m_Interval));
#else
            m_Timer = new Timer(DEFAULT_INTERVAL);
            m_Timer.Elapsed += new System.Timers.ElapsedEventHandler(callback);
#endif
        }

        /// <summary>
        /// Default contructor.
        /// </summary>
        /// <param name="interval">The time in milliseconds between events.</param>
        public TimerEx(TimerExCallback callback, double interval)
        {
#if NETSTANDARD
            m_Interval = interval;
            m_Timer = new Timer(WrapCallback(callback), null, NO_START, TimeSpan.FromMilliseconds(m_Interval));
#else
            m_Timer = new Timer(interval);
            m_Timer.Elapsed += new System.Timers.ElapsedEventHandler(callback);
#endif
        }

        /// <summary>
        /// Default contructor.
        /// </summary>
        /// <param name="autoReset">Specifies if timer is auto reseted.</param>
        public TimerEx(TimerExCallback callback, bool autoReset)
        {
#if NETSTANDARD
            m_AutoReset = autoReset;
            m_Timer = new Timer(WrapCallback(callback), null, NO_START, TimeSpan.FromMilliseconds(m_Interval));
#else
            m_Timer = new Timer(DEFAULT_INTERVAL);
            m_Timer.Elapsed += new System.Timers.ElapsedEventHandler(callback);
            m_Timer.AutoReset = autoReset;
#endif
        }

        /// <summary>
        /// Default contructor.
        /// </summary>
        /// <param name="interval">The time in milliseconds between events.</param>
        /// <param name="autoReset">Specifies if timer is auto reseted.</param>
        public TimerEx(TimerExCallback callback, double interval, bool autoReset)
        {
#if NETSTANDARD
            m_Interval = interval;
            m_AutoReset = autoReset;
            m_Timer = new Timer(WrapCallback(callback), null, NO_START, TimeSpan.FromMilliseconds(m_Interval));
#else
            m_Timer = new Timer(interval);
            m_Timer.Elapsed += new System.Timers.ElapsedEventHandler(callback);
            m_Timer.AutoReset = autoReset;
#endif
        }

        private Boolean m_Disposed = false;

        ~TimerEx()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
            {
                return;
            }

            if (disposing)
            {
                m_Timer?.Dispose();
            }

            m_Disposed = true;
        }


        // TODO: We need to do this class .NET CF compatible.


        public void Start()
        {
#if NETSTANDARD
            // always reset the auto-reset count on start
            m_AutoResetRun = 0;
            m_Timer.Change(START_NOW, TimeSpan.FromMilliseconds(m_Interval));
            m_IsRunning = true;
#else
            m_Timer.Start();
#endif
        }

        public void Stop()
        {
#if NETSTANDARD
            m_Timer.Change(NO_START, TimeSpan.FromMilliseconds(m_Interval));
            m_IsRunning = false;
#else
            m_Timer.Stop();
#endif
        }

        public Double Interval
        {
            get
            {
#if NETSTANDARD
                return m_Interval;
#else
                return m_Timer.Interval;
#endif
            }
            set
            {
#if NETSTANDARD
                m_Interval = value;
                if (m_IsRunning)
                {
                    m_Timer.Change(START_NOW, TimeSpan.FromMilliseconds(m_Interval));
                }
                else
                {
                    m_Timer.Change(NO_START, TimeSpan.FromMilliseconds(m_Interval));
                }
#else
                m_Timer.Interval = value;
#endif
            }
        }
    }
}
