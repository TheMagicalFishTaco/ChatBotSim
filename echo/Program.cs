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


    //Main function. Proceeds if the provided input returns true.
    private async Task InputResponse(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        int currState = turnState.Conversation.GetValue<int>("currState");
        var IsValidInput = CheckInput(turnContext.Activity.Text, turnState);
        if (IsValidInput)
        {
            switch(currState)
            {
                case 0:
                    await turnContext.SendActivityAsync($"What's your Mobile Number?", cancellationToken : cancellationToken);   
                    break;
                case 1:
                    await turnContext.SendActivityAsync($"What's your Address?", cancellationToken : cancellationToken); 
                    break;
                case 2:
                    await turnContext.SendActivityAsync($"Your information is:", cancellationToken : cancellationToken);      
                    await turnContext.SendActivityAsync(turnState.Conversation.GetValue<string>("InputName"), cancellationToken : cancellationToken);
                    await turnContext.SendActivityAsync(turnState.Conversation.GetValue<string>("InputMobileNum"), cancellationToken : cancellationToken);
                    await turnContext.SendActivityAsync(turnState.Conversation.GetValue<string>("InputAddress"), cancellationToken : cancellationToken);        
                    break;     
            }
            turnState.Conversation.SetValue("currState", currState + 1);
             
        }
        else
        {  
            string Name = turnState.Conversation.GetValue<string>("InputName");
            string Num = turnState.Conversation.GetValue<string>("InputMobileNum");
            string Address = turnState.Conversation.GetValue<string>("InputAddress");
            if (String.IsNullOrWhiteSpace(Name))
            {
                await turnContext.SendActivityAsync($"Error: Input your name again", cancellationToken : cancellationToken);  
            }
            else if (String.IsNullOrWhiteSpace(Num))
            {
                await turnContext.SendActivityAsync($"Error: Input your number again", cancellationToken : cancellationToken);  
            }
            else if (String.IsNullOrWhiteSpace(Address))
            {
                await turnContext.SendActivityAsync($"Error: Input your address again", cancellationToken : cancellationToken);  
            }
            
        }

    }


    //Handles checking the inputs through regex, uses the current state to determine which input needs to be validated.
    public bool CheckInput(string Input, ITurnState turnState)
    {
        int currState = turnState.Conversation.GetValue<int>("currState");
        Console.WriteLine($"CurrState = {currState}");
        switch(currState)
        {
            case 0:
            //Validate InputName
                if (Regex.IsMatch(Input, "^[A-Z]"))
                {
                    turnState.Conversation.SetValue("InputName", Input);
                    return true;
                }
                return false;
                
            case 1:
                //Validate MobileNum
                if (Regex.IsMatch(Input, @"^09\d{9}$"))
                {
                    turnState.Conversation.SetValue("InputMobileNum", Input);
                    return true;
                }
                return false;
            case 2:
                if (Regex.IsMatch(Input, "^[A-Z]"))
                {
                    turnState.Conversation.SetValue("InputAddress", Input);
                    return true;
                }
                return false;
            default:
                //Finish
                return true;
        }
    }

}


