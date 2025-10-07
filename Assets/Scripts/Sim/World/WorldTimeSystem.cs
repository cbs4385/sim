// Assets/Scripts/Sim/World/WorldTimeSystem.cs
// C# 8.0
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Sim.Config;

namespace Sim.World
{
    [Serializable]
    public class CalendarDefinition
    {
        public float tickRate = 10f;
        public int ticksPerDay = 240;
        public DayNightDefinition dayNight;
        public List<CalendarSeason> seasons;
    }

    [Serializable]
    public class DayNightDefinition
    {
        public int sunriseTick = 0;
        public int sunsetTick = 120;
    }

    [Serializable]
    public class CalendarSeason
    {
        public string name;
        public int lengthDays;
        public List<CalendarHoliday> holidays;
    }

    [Serializable]
    public class CalendarHoliday
    {
        public string name;
        public int day; // 1-based within the season
    }

    public sealed class WorldTimeSystem : MonoBehaviour
    {
        private const string DefaultCalendarPath = "Assets/Data/world/calendar.json";

        public string CalendarPath = DefaultCalendarPath;

        public event Action<int> Tick;
        public event Action<int> DayChanged;
        public event Action<CalendarSeasonState> SeasonChanged;
        public event Action<CalendarHoliday> HolidayStarted;

        private CalendarDefinition _definition;
        private readonly List<CalendarSeasonState> _seasons = new List<CalendarSeasonState>();
        private float _tickInterval = 0.1f;
        private float _accumulator;
        private int _ticksPerDay = 240;
        private int _tickOfDay;
        private int _dayOfYear;
        private int _seasonIndex;
        private int _daysInYear;

        public int TotalTicks { get; private set; }
        public float NormalizedTimeOfDay => _ticksPerDay > 0 ? _tickOfDay / (float)_ticksPerDay : 0f;
        public bool IsDaylight => _definition?.dayNight == null || (_tickOfDay >= _definition.dayNight.sunriseTick && _tickOfDay < _definition.dayNight.sunsetTick);
        public CalendarSeasonState CurrentSeason => _seasons.Count > 0 ? _seasons[_seasonIndex] : null;
        public int DayOfSeason => CurrentSeason == null ? 0 : (_dayOfYear - CurrentSeason.StartDay) + 1;
        public int DayOfYear => _dayOfYear + 1;

        private void Awake()
        {
            LoadDefinition();
            InitializeState();
        }

        private void Update()
        {
            _accumulator += Time.deltaTime;
            while (_accumulator >= _tickInterval)
            {
                _accumulator -= _tickInterval;
                AdvanceTick();
            }
        }

        private void LoadDefinition()
        {
            var path = string.IsNullOrWhiteSpace(CalendarPath) ? DefaultCalendarPath : CalendarPath;
            _definition = ConfigLoader.LoadJson<CalendarDefinition>(path);

            if (_definition == null)
                throw new InvalidDataException("Calendar definition missing");
            if (_definition.ticksPerDay <= 0)
                throw new InvalidDataException("calendar.json must define ticksPerDay > 0");
            if (_definition.seasons == null || _definition.seasons.Count == 0)
                throw new InvalidDataException("calendar.json must define at least one season");

            _tickInterval = 1f / Mathf.Max(0.1f, _definition.tickRate <= 0 ? 10f : _definition.tickRate);
            _ticksPerDay = _definition.ticksPerDay;

            BuildSeasonIndex(_definition.seasons);
        }

        private void BuildSeasonIndex(List<CalendarSeason> seasons)
        {
            _seasons.Clear();
            _daysInYear = 0;

            for (var i = 0; i < seasons.Count; i++)
            {
                var season = seasons[i];
                if (season == null)
                    throw new InvalidDataException($"Season at index {i} is null");
                if (season.lengthDays <= 0)
                    throw new InvalidDataException($"Season '{season.name}' must have lengthDays > 0");

                var state = new CalendarSeasonState(season, _daysInYear);
                _seasons.Add(state);
                _daysInYear += season.lengthDays;

                ValidateHolidays(state);
            }

            if (_daysInYear <= 0)
                throw new InvalidDataException("Calendar must cover at least one day");
        }

        private void ValidateHolidays(CalendarSeasonState season)
        {
            if (season.Definition?.holidays == null)
                return;

            foreach (var holiday in season.Definition.holidays)
            {
                if (holiday == null)
                    throw new InvalidDataException($"Season '{season.Definition.name}' has null holiday definition");
                if (holiday.day <= 0 || holiday.day > season.Definition.lengthDays)
                    throw new InvalidDataException($"Holiday '{holiday.name}' in season '{season.Definition.name}' has invalid day {holiday.day}");
            }
        }

        private void InitializeState()
        {
            TotalTicks = 0;
            _tickOfDay = 0;
            _dayOfYear = 0;
            _seasonIndex = 0;

            if (_seasons.Count == 0)
                return;

            var season = _seasons[_seasonIndex];
            UpdateCalendarState(season);
            WorldState.Calendar.LastHoliday = string.Empty;
            SeasonChanged?.Invoke(season);
        }

        private void AdvanceTick()
        {
            TotalTicks++;
            _tickOfDay++;

            if (_tickOfDay >= _ticksPerDay)
            {
                _tickOfDay = 0;
                AdvanceDay();
            }
            else
            {
                UpdateCalendarState(CurrentSeason);
            }

            Tick?.Invoke(TotalTicks);
        }

        private void AdvanceDay()
        {
            _dayOfYear++;
            if (_dayOfYear >= _daysInYear)
                _dayOfYear = 0;

            var newSeasonIndex = GetSeasonIndexForDay(_dayOfYear);
            if (newSeasonIndex != _seasonIndex)
            {
                _seasonIndex = newSeasonIndex;
                var season = _seasons[_seasonIndex];
                UpdateCalendarState(season);
                SeasonChanged?.Invoke(season);
            }
            else
            {
                UpdateCalendarState(CurrentSeason);
            }

            DayChanged?.Invoke(DayOfYear);
            WorldState.Calendar.LastHoliday = string.Empty;
            CheckForHoliday();
        }

        private int GetSeasonIndexForDay(int day)
        {
            for (var i = 0; i < _seasons.Count; i++)
            {
                var season = _seasons[i];
                if (day >= season.StartDay && day < season.EndDay)
                    return i;
            }

            return 0;
        }

        private void CheckForHoliday()
        {
            var season = CurrentSeason;
            if (season?.Definition?.holidays == null)
                return;

            var dayOfSeason = DayOfSeason;
            foreach (var holiday in season.Definition.holidays)
            {
                if (holiday.day == dayOfSeason)
                {
                    WorldState.Calendar.LastHoliday = holiday.name ?? string.Empty;
                    HolidayStarted?.Invoke(holiday);
                }
            }
        }

        private void UpdateCalendarState(CalendarSeasonState season)
        {
            WorldState.Calendar.TotalTicks = TotalTicks;
            WorldState.Calendar.TicksPerDay = _ticksPerDay;
            WorldState.Calendar.DayOfYear = DayOfYear;
            WorldState.Calendar.DayOfSeason = DayOfSeason;
            WorldState.Calendar.Season = season?.Definition?.name ?? string.Empty;
            WorldState.Calendar.IsDaylight = IsDaylight;
            WorldState.Calendar.NormalizedTimeOfDay = NormalizedTimeOfDay;
        }
    }

    public sealed class CalendarSeasonState
    {
        public CalendarSeason Definition { get; }
        public int StartDay { get; }
        public int EndDay { get; }

        public CalendarSeasonState(CalendarSeason definition, int startDay)
        {
            Definition = definition;
            StartDay = startDay;
            EndDay = startDay + (definition?.lengthDays ?? 0);
        }
    }
}
