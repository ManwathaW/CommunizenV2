﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{
    public class TimeSlot
    {
        public TimeSlot()
        {
            Id = Guid.NewGuid().ToString();
            IsAvailable = true;
        }

        [JsonProperty("Id")]
        public string Id { get; set; }

        [JsonProperty("StartTime")]
        public double StartTimeMinutes { get; set; }

        [JsonProperty("EndTime")]
        public double EndTimeMinutes { get; set; }

        [JsonIgnore]
        public TimeSpan StartTime
        {
            get => TimeSpan.FromMinutes(StartTimeMinutes);
            set => StartTimeMinutes = value.TotalMinutes;
        }

        [JsonIgnore]
        public TimeSpan EndTime
        {
            get => TimeSpan.FromMinutes(EndTimeMinutes);
            set => EndTimeMinutes = value.TotalMinutes;
        }

        [JsonProperty("IsAvailable")]
        public bool IsAvailable { get; set; }

        [JsonProperty("AppointmentId")]
        public string AppointmentId { get; set; }

        [JsonIgnore]
        public string DisplayTime => $"{StartTime:hh\\:mm tt} - {EndTime:hh\\:mm tt}";
    }
}

