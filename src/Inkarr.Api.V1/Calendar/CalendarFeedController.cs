using System;
using System.Collections.Generic;
using System.Linq;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Inkarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Tags;

namespace Inkarr.Api.V1.Calendar
{
    [V1FeedController("calendar")]
    public class CalendarFeedController : Controller
    {
        private readonly IIssueService _issueService;
        private readonly IVolumeService _volumeService;
        private readonly ITagService _tagService;

        public CalendarFeedController(IIssueService issueService, IVolumeService volumeService, ITagService tagService)
        {
            _issueService = issueService;
            _volumeService = volumeService;
            _tagService = tagService;
        }

        [HttpGet("Inkarr.ics")]
        public IActionResult GetCalendarFeed(int pastDays = 7, int futureDays = 28, string tagList = "", bool unmonitored = false)
        {
            var start = DateTime.Today.AddDays(-pastDays);
            var end = DateTime.Today.AddDays(futureDays);
            var tags = new List<int>();

            if (tagList.IsNotNullOrWhiteSpace())
            {
                tags.AddRange(tagList.Split(',').Select(_tagService.GetTag).Select(t => t.Id));
            }

            var issues = _issueService.IssuesBetweenDates(start, end, unmonitored);
            var calendar = new Ical.Net.Calendar
            {
                ProductId = "-//inkarr.com//Inkarr//EN"
            };

            var calendarName = "Inkarr Issue Schedule";
            calendar.AddProperty(new CalendarProperty("NAME", calendarName));
            calendar.AddProperty(new CalendarProperty("X-WR-CALNAME", calendarName));

            foreach (var issue in issues.OrderBy(v => v.ReleaseDate.Value))
            {
                var volume = _volumeService.GetVolume(issue.VolumeId); // Temp fix TODO: Figure out why Issue.Volume is not populated during IssuesBetweenDates Query

                if (tags.Any() && tags.None(volume.Tags.Contains))
                {
                    continue;
                }

                var occurrence = calendar.Create<CalendarEvent>();
                occurrence.Uid = "Inkarr_issue_" + issue.Id;

                //occurrence.Status = issue.HasFile ? EventStatus.Confirmed : EventStatus.Tentative;
                occurrence.Description = issue.Editions.Value.Single(x => x.Monitored).Overview;
                occurrence.Categories = issue.Genres;

                occurrence.Start = new CalDateTime(issue.ReleaseDate.Value.ToLocalTime()) { HasTime = false };
                occurrence.End = occurrence.Start;
                occurrence.IsAllDay = true;

                occurrence.Summary = $"{volume.Name} - {issue.Title}";
            }

            var serializer = (IStringSerializer)new SerializerFactory().Build(calendar.GetType(), new SerializationContext());
            var icalendar = serializer.SerializeToString(calendar);

            return Content(icalendar, "text/calendar");
        }
    }
}
