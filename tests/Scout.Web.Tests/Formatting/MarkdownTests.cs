using Scout.Web.Formatting;

namespace Scout.Web.Tests.Formatting;

public sealed class MarkdownTests
{
    [Fact]
    public void BasicFormattingIsRestrictedAndSourceStaysExact()
    {
        const string source="# Heading\n\n**bold** and *italic*\nnext\n\n- one\n- two\n\n1. first\n2. second\n\n> quote\n\n`<tag>`\n\n```mermaid\ngraph TD; A-->B\n```";
        var before=Scout.Web.Features.InterviewTranscripts.TranscriptOperations.TextHash(source);
        var result=MarkdownRenderer.Render(source);
        Assert.False(result.PlainTextFallback);
        foreach(var tag in new[]{"<h1>","<strong>","<em>","<ul>","<ol>","<li>","<blockquote>","<code>","<pre>","<br>"})Assert.Contains(tag,result.Html);
        Assert.Contains("&lt;tag&gt;",result.Html);Assert.Contains("graph TD",result.Html);Assert.DoesNotContain("class=",result.Html);
        Assert.Equal(before,Scout.Web.Features.InterviewTranscripts.TranscriptOperations.TextHash(source));
    }
    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("<svg onload=alert(1)><iframe src='https://evil.example'></iframe></svg>")]
    [InlineData("[click](javascript:alert%281%29) ![alt](https://evil.example/pixel)")]
    [InlineData("[x](data:text/html,evil) [x](file:///secret) <https://evil.example>")]
    [InlineData("[![nested](https://evil.example/pixel)](https://evil.example) **<style>p{display:none}</style>**")]
    public void HostileTextNeverProducesActiveTagsOrAttributes(string source)
    {
        var html=MarkdownRenderer.Render(source).Html;
        foreach(System.Text.RegularExpressions.Match tag in System.Text.RegularExpressions.Regex.Matches(html,"<[^>]+>"))
            Assert.Matches("^</?(p|h[1-6]|strong|em|ul|ol|li|blockquote|code|pre)>$|^<br>$",tag.Value);
    }
    [Fact]
    public void LinksAndImagesKeepOnlyTheirLabels()
    {
        var html=MarkdownRenderer.Render("[Read me](https://secret.example/link) ![Picture label](https://secret.example/pixel)").Html;
        Assert.Contains("Read me",html);Assert.Contains("Picture label",html);Assert.DoesNotContain("secret.example",html);
    }
    [Fact]
    public void LimitsPreserveCompleteTextAndMaximumTranscriptIsReadable()
    {
        var maximum=new string('a',1_000_000);
        var rendered=MarkdownRenderer.Render(maximum);Assert.Contains(maximum,rendered.Html);
        var excessive=maximum+"last character";var fallback=MarkdownRenderer.Render(excessive);Assert.True(fallback.PlainTextFallback);Assert.Contains(excessive,fallback.Html);
        var nested=string.Concat(Enumerable.Repeat("> ",100))+"deep";
        Assert.Contains("deep",MarkdownRenderer.Render(nested).Html);
        var pathological=new string('*',100_001)+"<script>";var safe=MarkdownRenderer.Render(pathological);Assert.True(safe.PlainTextFallback);Assert.Contains("&lt;script&gt;",safe.Html);
    }
}
