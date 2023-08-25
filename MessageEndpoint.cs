using System.Text;
using System.Security.Cryptography;
using OpenAI.Interfaces; // was OpenAI.GPT3.Interfaces
using OpenAI.ObjectModels; // was OpenAI.GPT3.ObjectModels
using OpenAI.ObjectModels.RequestModels; // was OpenAI.GPT3.ObjectModels.RequestModels
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
// using Microsoft.AspNetCore.Mvc.ModelBinding; // not used
// using OpenAI.ObjectModels; // redundancy

namespace SmsChatGpt;

public static class MessageEndpoint
{
    public static IEndpointRouteBuilder MapMessageEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/message", OnMessage);
        return builder;
    }

    private static async Task<IResult> OnMessage(
        HttpContext context,
        IOpenAIService openAiService,
        ITwilioRestClient twilioClient,
        CancellationToken cancellationToken
    )
    {
        var request = context.Request;

        var form = await request.ReadFormAsync(cancellationToken);
        var receivedFrom = form["From"].ToString();
        var sentTo = form["To"].ToString();
        var body = form["Body"].ToString().Trim();

        // ChatGPT doesn't need the phone number, just any string that uniquely identifies the user,
        // hence I'm hashing the phone number to not pass in PII unnecessarily
        var userId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(receivedFrom)));

        var completionResult = await openAiService.ChatCompletion.CreateCompletion(
            new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>()
                {
                    ChatMessage.FromUser(body)
                },
                // Model = Models.ChatGpt3_5Turbo, // depricated
                Model = Models.Gpt_3_5_Turbo,
                User = userId
            },
            cancellationToken: cancellationToken
        );

        if (!completionResult.Successful)
        {
            if (completionResult.Error == null) throw new Exception("An unexpected error occurred.");
            var errorMessage = completionResult.Error.Code ?? "";
            if (errorMessage != "") errorMessage += ": ";
            errorMessage += completionResult.Error.Message;
            throw new Exception(errorMessage);
        }

        var chatResponse = completionResult.Choices[0].Message.Content.Trim();

        await MessageResource.CreateAsync(
            to: receivedFrom,
            from: sentTo,
            body: chatResponse,
            client: twilioClient
        );

        return Results.Ok();
    }
}