using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System.Net.Mime;

namespace LaTeXForTeamsApp
{
    public class CardFactory
    {
        string AdaptiveCardJSON(string imgUri) => string.Format("{{ \"type\": \"AdaptiveCard\", \"$schema\": \"http://adaptivecards.io/schemas/adaptive-card.json\", \"version\": \"1.4\", \"body\": [ {{ \"type\": \"Image\", \"url\": \"{0}\", \"horizontalAlignment\": \"Center\", \"spacing\": \"None\" }} ] }}", imgUri);
        
        public Attachment MakeAdaptiveCard(string imgUri) => new()
        {
            ContentType = "application/vnd.microsoft.card.adaptive",
            Content = JsonConvert.DeserializeObject(AdaptiveCardJSON(imgUri)),
        };
    }
}
