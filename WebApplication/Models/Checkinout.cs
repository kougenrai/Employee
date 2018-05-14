using System;

namespace WebApplication.Models
{
    public class Checkinout
    {
        public string Checkin { get; set; }
        public string Checkout { get; set; }
        public TimeSpan WorkTime { get; private set; }
        public TimeSpan FreeTime { get; private set; }
    }
}