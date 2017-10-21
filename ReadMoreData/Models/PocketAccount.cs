using System;
using System.Diagnostics.CodeAnalysis;

namespace ReadMoreData.Models
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class PocketAccount
    {
        public Guid Id { get; set; }
        public string RedirectUrl { get; set; }
        public string RequestToken { get; set; }
        public string AccessToken { get; set; }
        public string Username { get; set; }
        public Guid EmailUserId { get; set; }
    }
}
