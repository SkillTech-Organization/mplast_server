using System.Collections.Generic;

namespace MPWeb.Logic.BLL
{
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsAuthenticated { get; set; }
        public List<string> Roles { get; set; }
    }
}
