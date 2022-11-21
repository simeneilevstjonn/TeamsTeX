using CSharpMath.Rendering.FrontEnd;
using LaTeXForTeamsApp.Exceptions;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Newtonsoft.Json.Linq;

namespace LaTeXForTeamsApp.Bot;

public class TeamsMessageExtension : TeamsActivityHandler
{
    // Message Extension Code
    // Action.
    protected override Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionSubmitActionAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
    {
        switch (action.CommandId)
        {
            case "createCard":
                return CreateCardCommand(turnContext, action);
        }
        return Task.FromResult(new MessagingExtensionActionResponse());
    }

    private async Task<MessagingExtensionActionResponse> CreateCardCommand(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action)
    {
        // The user has chosen to create a card by choosing the 'Create Card' context menu command.
        var createCardData = ((JObject)action.Data).ToObject<CardResponse>();

        LatexRenderer renderer = new("C:/Users/Simen/AppData/Local/Temp");
        List<MessagingExtensionAttachment> attachments = new();

        try 
        {
            (string, bool) img = await renderer.LatexToPngString(createCardData.Latex);
            string url = "data:image/png;base64," + img.Item1;
            
            CardFactory cardFactory = new();
            Attachment adaptCard = cardFactory.MakeAdaptiveCard(url, img.Item2);

            MessagingExtensionAttachment attach = AttachmentExtensions.ToMessagingExtensionAttachment(adaptCard, adaptCard);

            attachments.Add(attach);
        }
        catch (LatexException) 
        {
            // Failed to parse latex
            HeroCard card = new()
            {
                Title = "LaTeX error",
                Text = "Failed to parse LaTeX, please check validity"
            };

            attachments.Add(new()
            {
                Content = card,
                ContentType = HeroCard.ContentType,
                Preview = card.ToAttachment()
            });
        }

        return new MessagingExtensionActionResponse
        {
            ComposeExtension = new MessagingExtensionResult
            {
                AttachmentLayout = "list",
                Type = "result",
                Attachments = attachments,
            },
        };
    }

    internal class CardResponse
    {
        public string Latex { get => Dollar ? Text : string.Format("${0}$", Text); }
        public string Text { get; set; }
        public bool Dollar { get; set; }
    }
}

