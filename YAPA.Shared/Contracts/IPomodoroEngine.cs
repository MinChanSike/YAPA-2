﻿using System;
using System.ComponentModel;

namespace YAPA.Shared.Contracts
{
    public interface IPomodoroEngine : INotifyPropertyChanged
    {
        int Index { get; }
        PomodoroPhase Phase { get; }
        int Elapsed { get; }
        int Remaining { get; }
        int DisplayValue { get; }
        int CurrentIntervalLength { get; }
        int WorkTime { get; }
        int BreakTime { get; }

        event Func<bool> OnStarting;
        event Action OnStarted;
        event Action OnStopped;
        event Action OnPaused;
        event Action OnPomodoroCompleted;

        void Start();
        void Stop();
        void Pause();
        void Reset();

        bool IsRunning { get; }
    }

    public enum PomodoroPhase
    {
        NotStarted,
        Work,
        WorkEnded,
        Break,
        BreakEnded,
        Pause
    }
}
