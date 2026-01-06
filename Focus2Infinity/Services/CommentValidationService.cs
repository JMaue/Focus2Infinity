using Anthropic.SDK;
using Microsoft.Extensions.AI;

namespace Focus2Infinity.Services
{
  public interface ICommentValidationService
  {
    Task<(bool, string)> IsValidComment(string comment);
  }

  public class CommentValidationService : ICommentValidationService
  {
    private readonly IChatClient _anthropicClient;
    private readonly ChatOptions _chatOptions;

    private const string ModerationPrompt = @"You are a content‑moderation system for a public astronomy website. 
Your task is to evaluate a user-submitted comment and decide whether it is appropriate for display.

Analyze the comment strictly according to the following rules:

A comment is INAPPROPRIATE if it contains:
- Hate speech, insults, harassment, or abusive language
- Threats, encouragement of violence, or dangerous behavior
- Sexual content, explicit or suggestive language
- Spam, advertisements, or promotional links
- Personal data about someone else (addresses, phone numbers, etc.)
- Discriminatory or extremist content
- Illegal content or instructions for illegal activities
- Irrelevant nonsense, trolling, or attempts to break the system

A comment is APPROPRIATE if it:
- Is respectful, safe, and relevant to astronomy, images, science, or general conversation
- Contains mild criticism or disagreement without harassment
- Contains harmless jokes or casual language

Your output must be a JSON object with the following fields:

{{
  ""appropriate"": true/false,
  ""reason"": ""Short explanation of why the comment is or is not appropriate.""
}}

Now evaluate the following user comment: ";

    public CommentValidationService(string key)
    {
      _anthropicClient = new AnthropicClient(
        new APIAuthentication(key))
        .Messages
        .AsBuilder()
        .Build();

      _chatOptions = new ChatOptions
      {
        MaxOutputTokens = 1000,
        ModelId = "claude-haiku-4-5-20251001"
      };
    }

    public async Task<(bool, string)> IsValidComment(string comment)
    {
      // Check if the comment is null or empty
      if (string.IsNullOrWhiteSpace(comment))
        return (false, "Comment is required.");

      // Check for length constraints (e.g., max 500 characters)
      if (comment.Length > 500)
        return (false, "Comment is too long.");

      var msg = AssembleChatMessage(comment);
      string responseContent = string.Empty;
      await foreach (var message in _anthropicClient.GetStreamingResponseAsync(msg, _chatOptions))
      {
        responseContent += message.Text;
      }

      return ParseModerationResponse(responseContent);
    }

    private (bool, string) ParseModerationResponse(string responseContent)
    {
      var reason = "undefined";
      bool isAppropriate = false;
      try
      {
        // Remove markdown code block markers if present
        string cleanedContent = responseContent.Trim();
        if (cleanedContent.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
          cleanedContent = cleanedContent.Substring(7).Trim();
        }
        else if (cleanedContent.StartsWith("```"))
        {
          cleanedContent = cleanedContent.Substring(3).Trim();
        }
        
        if (cleanedContent.EndsWith("```"))
        {
          cleanedContent = cleanedContent.Substring(0, cleanedContent.Length - 3).Trim();
        }

        var json = System.Text.Json.JsonDocument.Parse(cleanedContent);

        if (json.RootElement.TryGetProperty("reason", out var reasonElement))
        {
          reason = reasonElement.GetString() ?? "unknown";
        }
        if (json.RootElement.TryGetProperty("appropriate", out var appropriateElement))
        {
          isAppropriate = appropriateElement.GetBoolean();
        }
        else
        {
          // If the "appropriate" field is missing, consider the comment invalid
          reason = "appropriate field is missing";
        }
      }
      catch (System.Text.Json.JsonException ex)
      {
        // If JSON parsing fails, consider the comment invalid
        reason = $"JSON parsing failed: {ex.Message}";
      }

      return (isAppropriate, reason);
    }

    private ChatMessage AssembleChatMessage(string comment)
    {
      string prompt = ModerationPrompt + comment;
      return new ChatMessage(ChatRole.User, prompt);
    }
  }
}
