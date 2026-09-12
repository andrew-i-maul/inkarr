using System.Collections.Generic;

namespace Inkarr.Api.V1.Author
{
    public class AuthorEditorDeleteResource
    {
        public List<int> AuthorIds { get; set; }
        public bool DeleteFiles { get; set; }
    }
}
