using Inkarr.Api.V1.Issues;
using Inkarr.Api.V1.Volume;
using Inkarr.Http;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Parser;

namespace Inkarr.Api.V1.Parse
{
    [V1ApiController]
    public class ParseController : Controller
    {
        private readonly IParsingService _parsingService;

        public ParseController(IParsingService parsingService)
        {
            _parsingService = parsingService;
        }

        [HttpGet]
        public ParseResource Parse(string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return null;
            }

            var parsedIssueInfo = Parser.ParseIssueTitle(title);

            if (parsedIssueInfo == null)
            {
                return new ParseResource
                {
                    Title = title
                };
            }

            var remoteIssue = _parsingService.Map(parsedIssueInfo);

            if (remoteIssue != null)
            {
                return new ParseResource
                {
                    Title = title,
                    ParsedIssueInfo = remoteIssue.ParsedIssueInfo,
                    Volume = remoteIssue.Volume.ToResource(),
                    Issues = remoteIssue.Issues.ToResource()
                };
            }
            else
            {
                return new ParseResource
                {
                    Title = title,
                    ParsedIssueInfo = parsedIssueInfo
                };
            }
        }
    }
}
