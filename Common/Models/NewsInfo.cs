using System;

namespace Common.Models
{
    /// <summary>
    /// 뉴스 정보 — 불변(Immutable)
    /// </summary>
    public sealed class NewsInfo
    {
        public string NewsId { get; }
        public string Title { get; }
        public string Content { get; }
        public string Source { get; }
        public string RelatedCode { get; }
        public DateTime PublishTime { get; }

        public NewsInfo(string newsId, string title, string content,
            string source, string relatedCode, DateTime publishTime)
        {
            NewsId = newsId ?? "";
            Title = title ?? "";
            Content = content ?? "";
            Source = source ?? "";
            RelatedCode = relatedCode ?? "";
            PublishTime = publishTime;
        }

        public override string ToString() =>
            $"[{PublishTime:HH:mm}] {Title} ({Source})";
    }
}