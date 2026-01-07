using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<CommentValidationService> _logger;

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
The short explanation of the reason must be in the same language as the user-submitted comment
Now evaluate the following user comment: ";

    public CommentValidationService(string apiKey, ILogger<CommentValidationService> logger)
    {
      _logger = logger;
      _logger.LogInformation("Initializing CommentValidationService");

      // Create HttpClient with custom timeout
      var httpClient = new HttpClient
      {
        Timeout = TimeSpan.FromSeconds(120) // Set timeout to 120 seconds (default is 100 seconds)
      };

      _anthropicClient = new AnthropicClient(
        new APIAuthentication(apiKey),
        httpClient)
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
      _logger.LogInformation("Validating comment with length: {Length}", comment?.Length ?? 0);
      
      // Check if the comment is null or empty
      if (string.IsNullOrWhiteSpace(comment))
      {
        _logger.LogWarning("Comment validation failed: empty comment");
        return (false, "Comment is empty");
      }

      // Check for length constraints (e.g., max 500 characters)
      if (comment.Length > 500)
      {
        _logger.LogWarning("Comment validation failed: length {Length} exceeds limit", comment.Length);
        return (false, "Comment too long");
      }

      try
      {
        var msg = AssembleChatMessage(comment);
        string responseContent = string.Empty;
        
        await foreach (var message in _anthropicClient.GetStreamingResponseAsync(msg, _chatOptions))
        {
          _logger.LogWarning("received from Anthropic API: {Length}", message.Text.Length);
          responseContent += message.Text;
        }

        return ParseModerationResponse(responseContent);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error validating comment");
        return (false, "Validation error occurred");
      }
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

      _logger.LogInformation("Comment moderation result: appropriate={IsAppropriate}, reason={Reason}", isAppropriate, reason);
      return (isAppropriate, reason);
    }

    private ChatMessage AssembleChatMessage(string comment)
    {
      string prompt = ModerationPrompt + comment;
      return new ChatMessage(ChatRole.User, prompt);
    }
  }
}
