namespace Focus2Infinity.Data
{
  public sealed class CommentItem
  {
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Reason { get; set; } = string.Empty;
  }
}
