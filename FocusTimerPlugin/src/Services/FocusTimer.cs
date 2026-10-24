namespace Loupedeck.FocusTimerPlugin
{
    using System;
    using System.Threading;

    // A countdown timer shared by all the plugin actions.
    // It does not depend on the plugin API, so the actions only have to react to `StateChanged`.

    public class FocusTimer : IDisposable
    {
        public const Int32 MinMinutes = 1;
        public const Int32 MaxMinutes = 90;
        public const Int32 DefaultMinutes = 25;

        private readonly Object _lock = new Object();
        private readonly Timer _ticker;

        private Int32 _durationMinutes = DefaultMinutes;
        private TimeSpan _remaining = TimeSpan.FromMinutes(DefaultMinutes);
        private DateTime _lastTickUtc;
        private Boolean _isRunning;

        // Raised (on a background thread) whenever the remaining time, duration, or running state changes.
        public event EventHandler StateChanged;

        // Raised once when the countdown reaches zero.
        public event EventHandler Finished;

        public FocusTimer() => this._ticker = new Timer(this.OnTick, null, Timeout.Infinite, Timeout.Infinite);

        public Int32 DurationMinutes
        {
            get { lock (this._lock) { return this._durationMinutes; } }
        }

        public TimeSpan Remaining
        {
            get { lock (this._lock) { return this._remaining; } }
        }

        public Boolean IsRunning
        {
            get { lock (this._lock) { return this._isRunning; } }
        }

        // Sets a new duration. If the timer is not running, the remaining time is reset to the new duration.
        public void SetDuration(Int32 minutes)
        {
            lock (this._lock)
            {
                this._durationMinutes = Math.Clamp(minutes, MinMinutes, MaxMinutes);
                if (!this._isRunning)
                {
                    this._remaining = TimeSpan.FromMinutes(this._durationMinutes);
                }
            }

            this.RaiseStateChanged();
        }

        public void Start()
        {
            lock (this._lock)
            {
                if (this._isRunning)
                {
                    return;
                }

                if (this._remaining <= TimeSpan.Zero)
                {
                    this._remaining = TimeSpan.FromMinutes(this._durationMinutes);
                }

                this._isRunning = true;
                this._lastTickUtc = DateTime.UtcNow;
                this._ticker.Change(1000, 1000);
            }

            this.RaiseStateChanged();
        }

        public void Pause()
        {
            lock (this._lock)
            {
                if (!this._isRunning)
                {
                    return;
                }

                this.UpdateRemaining();
                this.StopTicker();
            }

            this.RaiseStateChanged();
        }

        public void Toggle()
        {
            if (this.IsRunning)
            {
                this.Pause();
            }
            else
            {
                this.Start();
            }
        }

        // Stops the timer and restores the full duration.
        public void Reset()
        {
            lock (this._lock)
            {
                this.StopTicker();
                this._remaining = TimeSpan.FromMinutes(this._durationMinutes);
            }

            this.RaiseStateChanged();
        }

        public void Dispose()
        {
            this._ticker.Dispose();
            GC.SuppressFinalize(this);
        }

        private void OnTick(Object state)
        {
            var finished = false;

            lock (this._lock)
            {
                if (!this._isRunning)
                {
                    return;
                }

                this.UpdateRemaining();
                if (this._remaining <= TimeSpan.Zero)
                {
                    this._remaining = TimeSpan.Zero;
                    this.StopTicker();
                    finished = true;
                }
            }

            this.RaiseStateChanged();
            if (finished)
            {
                this.Finished?.Invoke(this, EventArgs.Empty);
            }
        }

        // Must be called while holding the lock. Uses wall-clock time so missed ticks do not cause drift.
        private void UpdateRemaining()
        {
            var now = DateTime.UtcNow;
            this._remaining -= now - this._lastTickUtc;
            this._lastTickUtc = now;
        }

        // Must be called while holding the lock.
        private void StopTicker()
        {
            this._isRunning = false;
            this._ticker.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void RaiseStateChanged() => this.StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
