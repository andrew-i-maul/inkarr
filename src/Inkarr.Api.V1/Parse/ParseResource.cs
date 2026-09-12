using System.Collections.Generic;
using Inkarr.Api.V1.Author;
using Inkarr.Api.V1.Books;
using Inkarr.Http.REST;
using NzbDrone.Core.Parser.Model;

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
