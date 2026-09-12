using System.Collections.Generic;
using NzbDrone.Core.Parser.Model;
using Inkarr.Api.V1.Author;
using Inkarr.Api.V1.Books;
using Inkarr.Http.REST;

namespace Inkarr.Api.V1.Parse
{
    public class ParseResource : RestResource
    {
        public string Title { get; set; }
        public ParsedBookInfo ParsedBookInfo { get; set; }
        public AuthorResource Author { get; set; }
        public List<BookResource> Books { get; set; }
    }
}
