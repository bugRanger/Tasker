namespace Redmine2Trello.Services
{
    using CommandLine;

    class TrelloOptions : ITrelloOptions
    {
        [Option("tr_apikey", Required = true, ResourceType = typeof(string))]
        public string AppKey { get; set; }

        [Option("tr_token", Required = true, ResourceType = typeof(string))]
        public string Token { get; set; }
    }
}
