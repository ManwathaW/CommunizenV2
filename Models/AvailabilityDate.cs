﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{
    public class AvailabilityDate
    {
        public string Id { get; set; }
        public string PractitionerId { get; set; }
        public DateTime Date { get; set; }
        public List<TimeSlot> TimeSlots { get; set; } = new();
        public bool IsAvailable => TimeSlots.Any(x => x.IsAvailable);
    }

    public enum AppointmentStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Completed
    }
}
