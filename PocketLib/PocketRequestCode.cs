using System;

namespace PocketLib
{
    public class PocketRequestCode
    {
        private readonly Uri _baseUri;
        private readonly Uri _redirectUri;
        public string Code { get; }
        
        public PocketRequestCode(Uri baseUri, Uri redirectUri, string requestCode)
        {
            _baseUri = baseUri;
            _redirectUri = redirectUri;
            Code = requestCode;
        }

        public Uri ToAuthUrl()
        {
            return new Uri($"{_baseUri}auth/authorize?request_token={Code}&redirect_uri={_redirectUri}");
        }
    }
}