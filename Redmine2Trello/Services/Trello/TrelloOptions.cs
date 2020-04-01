﻿namespace Redmine2Trello.Services
{
    using CommandLine;

    class TrelloOptions : ITrelloOptions, ITrelloSync
    {
        [Option("tr_apikey", Required = true, ResourceType = typeof(string))]
        public string AppKey { get; set; }

        [Option("tr_token", Required = true, ResourceType = typeof(string))]
        public string Token { get; set; }
        
        ITrelloSync ITrelloOptions.Sync => this;

        [Option("tr_interval", Required = true, ResourceType = typeof(string))]
        public int Interval { get; set; }

        [Option("tr_boards", Required = true, ResourceType = typeof(string[]))]
        public string[] BoardIds { get; set; }
    }
}
