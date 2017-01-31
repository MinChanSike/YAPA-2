﻿using System;
using System.Linq;

namespace YAPA.Contracts
{
    public interface IPomodoroRepository
    {
        IQueryable<PomodoroEntity> Pomodoros { get; }

        void Delete(int id);
        void Add(PomodoroEntity pomo);
    }

    public class PomodoroEntity
    {
        public int Id { get; set; }

        public DateTime DateTime { get; set; }

        public int Count { get; set; }
    }
}
