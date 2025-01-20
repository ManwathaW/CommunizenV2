using CommuniZEN.Interfaces;
using CommuniZEN.Services;
using CommuniZEN.Models;

using CommuniZEN.Interfaces;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;
using OpenAI;
using CommuniZEN.Interfaces;
using OpenAI.ObjectModels;

namespace CommuniZEN.Services
{
    public class OpenAIChatbotService : IChatbotService
    {
        private readonly OpenAIService _openAIService;

        public OpenAIChatbotService(string apiKey)
        {
            _openAIService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = apiKey
            });
        }

        public async Task<string> GetResponseAsync(string userMessage)
        {
            try
            {
                var completionResult = await _openAIService.ChatCompletion.CreateCompletion(new OpenAI.ObjectModels.RequestModels.ChatCompletionCreateRequest
                {
                    Messages = new List<OpenAI.ObjectModels.RequestModels.ChatMessage>
                    {
                        new OpenAI.ObjectModels.RequestModels.ChatMessage("user", userMessage)
                    },
                    Model = OpenAI.ObjectModels.Models.Gpt_3_5_Turbo
                });

                if (completionResult.Successful)
                {
                    return completionResult.Choices.First().Message.Content;
                }
                else
                {
                    return $"Error: {completionResult.Error?.Message}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}