namespace PocketLib
{
    public class PocketRequestCode
    {
        private readonly string _baseUri;
        private readonly string _redirectUri;
        public string Code { get; }
        
        public PocketRequestCode(string baseUri, string redirectUri, string requestCode)
        {
            _baseUri = baseUri;
            _redirectUri = redirectUri;
            Code = requestCode;
        }

        public string ToAuthUrl()
        {
            return $"{_baseUri}/auth/authorize?request_token={Code}&redirect_uri={_redirectUri}";
        }
    }
}