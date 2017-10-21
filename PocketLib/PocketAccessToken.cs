using System;
using System.Collections.Generic;
using System.Text;

namespace PocketLib
{
    public class PocketAccessToken
    {
        public PocketAccessToken(string username, string accessToken)
        {
            Username = username;
            AccessToken = accessToken;
        }

        public string AccessToken { get; }
        public string Username { get; }
    }
}
