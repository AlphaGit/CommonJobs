﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonJobs.Domain.MyMenu
{
    public class DailyMenuRequest
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public string MenuId { get; set; }
        public int WeekIdx { get; set; }
        public int DayIdx { get; set; }
        public Dictionary<string, string> PlacesByKey { get; set; }
        public Dictionary<string, string> OptionsByKey { get; set; }
        public Dictionary<string, string> FoodsByOption { get; set; }
        public Dictionary<string, Dictionary<string, int>> QuantityByOptionByPlace { get; set; }
        public Dictionary<string, DailyMenuRequestDetailItem> DetailByUserName { get; set; }

        private DailyMenuRequest()
        {
            //RavenDB use
            DetailByUserName = new Dictionary<string, DailyMenuRequestDetailItem>();
            QuantityByOptionByPlace = new Dictionary<string, Dictionary<string, int>>();
            PlacesByKey = new Dictionary<string, string>();
            OptionsByKey = new Dictionary<string, string>();
            FoodsByOption = new Dictionary<string, string>();
        }

        public DailyMenuRequest(Menu menu, DateTime date, IEnumerable<EmployeeMenu> employeeMenues)
        {
            Date = date.Date;
            Id = string.Format("{0}/{1:yyyy-MM-dd}", menu.Id, Date);
            MenuId = menu.Id;

            DetailByUserName = new Dictionary<string, DailyMenuRequestDetailItem>();
            PlacesByKey = menu.Places.ToDictionary(x => x.Key, x => x.Text);
            OptionsByKey = menu.Options.ToDictionary(x => x.Key, x => x.Text);

            var dayWeek = menu.GetWeekDay(Date);
            DayIdx = dayWeek.DayIdx;
            WeekIdx = dayWeek.WeekIdx;

            FoodsByOption = OptionsByKey
                .Select(x => menu.Foods.GetItemSecurely(new WeekDayOptionKey() { WeekIdx = WeekIdx, DayIdx = DayIdx, OptionKey = x.Key }))
                .Where(x => !string.IsNullOrWhiteSpace(x.Food))
                .ToDictionary(x => x.OptionKey, x => x.Food);

            employeeMenues = employeeMenues ?? Enumerable.Empty<EmployeeMenu>();

            foreach (var employeeMenu in employeeMenues)
            {
                DetailByUserName[employeeMenu.UserName] = CreateDetailItem(employeeMenu);
            }

            QuantityByOptionByPlace = DetailByUserName.Values
                .Where(x => IsNotEmpty(x.PlaceKey) && IsNotEmpty(x.OptionKey))
                .GroupBy(x => x.PlaceKey)
                .ToDictionary(
                    x => x.Key, 
                    x => x.GroupBy(y => y.OptionKey).ToDictionary(y => y.Key, y => y.Count()));
        }

        #region private helper methods
        private bool IsNotEmpty(string key)
        {
            return !string.IsNullOrWhiteSpace(key);
        }

        private bool IsValidPlace(string placeKey)
        {
            return PlacesByKey.ContainsKey(placeKey);
        }

        private bool IsNotEmptyValidPlace(string placeKey)
        {
            return IsNotEmpty(placeKey) && IsValidPlace(placeKey);
        }

        private string GetPlaceOrDefault(string placeKey, string defaultValue)
        {
            return IsNotEmptyValidPlace(placeKey) ? placeKey : defaultValue;
        }

        private bool IsValidOption(string optionKey)
        {
            return OptionsByKey.ContainsKey(optionKey) && FoodsByOption.ContainsKey(optionKey) && !string.IsNullOrWhiteSpace(FoodsByOption[optionKey]);
        }

        private bool IsNotEmptyValidOption(string optionKey)
        {
            return IsNotEmpty(optionKey) && IsValidOption(optionKey);
        }

        private string GetOptionOrDefault(string optionKey, string defaultValue)
        {
            return IsNotEmptyValidOption(optionKey) ? optionKey : defaultValue;
        }


        #endregion

        private DailyMenuRequestDetailItem CreateDetailItem(EmployeeMenu employeeMenu)
        {
            string placeKey = null;
            string optionKey = null;

            placeKey = GetPlaceOrDefault(employeeMenu.DefaultPlaceKey, placeKey);

            var dayChoices = employeeMenu.WeeklyChoices.GetItemSecurely(new WeekDayKey() { WeekIdx = WeekIdx, DayIdx = DayIdx });

            placeKey = GetPlaceOrDefault(dayChoices.PlaceKey, placeKey);
            optionKey = GetOptionOrDefault(dayChoices.OptionKey, optionKey);

            var lastOverride = employeeMenu.Overrides.Where(x => x.Date.Date == Date).LastOrDefault();

            if (!lastOverride.Cancel)
            {
                placeKey = GetPlaceOrDefault(lastOverride.PlaceKey, placeKey);
                optionKey = GetOptionOrDefault(lastOverride.OptionKey, optionKey);
            }

            if (lastOverride.Cancel || string.IsNullOrWhiteSpace(placeKey) || string.IsNullOrWhiteSpace(optionKey))
            {
                placeKey = null;
                optionKey = null;
            }

            var comment = IsNotEmpty(lastOverride.Comment) ? lastOverride.Comment : null;

            return new DailyMenuRequestDetailItem()
            {
                EmployeeName = employeeMenu.EmployeeName,
                PlaceKey = placeKey,
                OptionKey = optionKey,
                Comment = comment
            };
        }
    }
}
