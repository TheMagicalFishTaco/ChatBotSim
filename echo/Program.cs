using System.Text.RegularExpressions;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualBasic;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.AddAgentApplicationOptions();
builder.AddAgent<EchoAgent>();
builder.Services.AddSingleton<IStorage, MemoryStorage>();

var app = builder.Build();


app.MapPost("/api/messages",async(HttpRequest request, HttpResponse response, IAgentHttpAdapter adapter, IAgent agent, CancellationToken cancellationToken) =>
{
   await adapter.ProcessAsync(request, response, agent, cancellationToken); 
});
app.Run();


public class EchoAgent : AgentApplication
{
    private string InputName;
    private string InputMobileNum;
    private string InputAddress;
    private int currState = 0;

    public EchoAgent(AgentApplicationOptions options) : base(options)
    {
        OnConversationUpdate(ConversationUpdateEvents.MembersAdded, InputCall);
        OnActivity(ActivityTypes.Message, InputResponse, rank : RouteRank.Last);
    }

    private async Task InputCall(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        foreach (ChannelAccount member in turnContext.Activity.MembersAdded)
        if (member.Id != turnContext.Activity.Recipient.Id)
        {
            await turnContext.SendActivityAsync(MessageFactory.Text("What's your Name?"), cancellationToken);
        }
    }
    private async Task InputResponse(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        var IsValidInput = CheckInput(turnContext.Activity.Text);
        if (IsValidInput)
        {
            await turnContext.SendActivityAsync($"Name is: {InputName}", cancellationToken : cancellationToken);            
        }
        else
        {
            await turnContext.SendActivityAsync($"Error: Input your name again", cancellationToken : cancellationToken);  
        }

    }

    public bool CheckInput(string Input)
    {
        switch(currState)
        {
            case 0:
            //Validate InputName
                if (Regex.IsMatch(Input, "^[A-Z]"))
                {
                    InputName = Input;
                    currState += 1;
                    return true;
                }
                return false;
                
            case 1:
                //Validate MobileNum
            case 2:
                //Validate Address
            default:
                //Finish
                return false;
        }
    }

}


