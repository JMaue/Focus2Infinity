namespace Focus2Infinity.Services
{
  using Focus2Infinity.Data;
  using System.Text.Json;

  public class CommentService
  {
    private readonly ImagePathResolver _pathResolver;

    public CommentService(ImagePathResolver pathResolver)
    {
      _pathResolver = pathResolver;
    }

    public async Task<List<CommentItem>> GetCommentHistory(string topic, string src)
    {
      var rc = new List<CommentItem>();
      string commentsFilePath = _pathResolver.GetCommentsPath(topic, src);
      if (File.Exists(commentsFilePath))
      {
        await Task.Run(() =>
        {
          var jsonString = File.ReadAllText(commentsFilePath);
          rc = JsonSerializer.Deserialize<List<CommentItem>>(jsonString) ?? new List<CommentItem>();
        });
      }
      return rc;
    }

    public async Task AddComment(string topic, string src, CommentItem comment, bool isValid)
    {
      string commentsFilePath = isValid ?
          _pathResolver.GetCommentsPath(topic, src) :
          _pathResolver.GetDeniedCommentsPath(topic, src);

      var comments = new List<CommentItem>();
      if (File.Exists(commentsFilePath))
      {
        var jsonString = await File.ReadAllTextAsync(commentsFilePath);
        comments = JsonSerializer.Deserialize<List<CommentItem>>(jsonString) ?? new List<CommentItem>();
      }
      comments.Add(comment);
      var updatedJson = JsonSerializer.Serialize(comments, new JsonSerializerOptions { WriteIndented = true });
      await File.WriteAllTextAsync(commentsFilePath, updatedJson);
    }
  }
}
