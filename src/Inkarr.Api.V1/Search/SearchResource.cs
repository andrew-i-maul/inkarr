using Inkarr.Api.V1.Author;
using Inkarr.Api.V1.Books;
using Inkarr.Http.REST;

namespace Inkarr.Api.V1.Search
{
    public class SearchResource : RestResource
    {
        public string ForeignId { get; set; }
        public AuthorResource Author { get; set; }
        public BookResource Book { get; set; }
    }
}
