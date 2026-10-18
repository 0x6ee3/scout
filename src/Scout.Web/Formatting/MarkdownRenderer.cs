using System.Diagnostics;
using System.Net;
using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Scout.Web.Formatting;

public sealed record MarkdownResult(string Html, bool PlainTextFallback);

public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder { MaximumNestingDepth = 32 }.DisableHtml().Build();
    public const int MaximumCharacters = 1_000_000;
    private const int MaximumNodes = 100_000;

    public static MarkdownResult Render(string? source)
    {
        source ??= "";
        if(source.Length > MaximumCharacters || source.Count(c=>c is '*' or '_' or '[' or ']' or '>' or '`') > 100_000) return Plain(source);
        try
        {
            var watch=Stopwatch.StartNew();
            var document=Markdown.Parse(source,Pipeline);
            var output=new StringBuilder();var nodes=0;
            void Visit(MarkdownObject node,int depth)
            {
                if(depth>32 || ++nodes>MaximumNodes || watch.ElapsedMilliseconds>500 || output.Length>8*MaximumCharacters) throw new RenderBudgetException();
                string? tag=node switch
                {
                    HeadingBlock h => "h"+h.Level,
                    ParagraphBlock => "p", QuoteBlock => "blockquote", ListItemBlock => "li",
                    ListBlock list => list.IsOrdered ? "ol" : "ul",
                    EmphasisInline e => e.DelimiterCount==2 ? "strong" : "em",
                    _ => null
                };
                if(tag is not null)output.Append('<').Append(tag).Append('>');
                switch(node)
                {
                    case CodeBlock code: output.Append("<pre><code>").Append(WebUtility.HtmlEncode(code.Lines.ToString())).Append("</code></pre>");break;
                    case CodeInline code: output.Append("<code>").Append(WebUtility.HtmlEncode(code.Content)).Append("</code>");break;
                    case LiteralInline literal: output.Append(WebUtility.HtmlEncode(literal.Content.ToString()));break;
                    case HtmlEntityInline entity: output.Append(WebUtility.HtmlEncode(entity.Transcoded.ToString()));break;
                    case LineBreakInline: output.Append("<br>");break;
                    case AutolinkInline link: output.Append(WebUtility.HtmlEncode(link.Url));break;
                    // Links and images deliberately emit only their child labels: never destinations or attributes.
                    case ContainerInline inline: for(var child=inline.FirstChild;child is not null;child=child.NextSibling)Visit(child,depth+1);break;
                    case LeafBlock leaf when leaf.Inline is not null: Visit(leaf.Inline,depth+1);break;
                    case ContainerBlock block: foreach(var child in block)Visit(child,depth+1);break;
                }
                if(tag is not null)output.Append("</").Append(tag).Append('>');
            }
            Visit(document,0);
            return new(output.ToString(),false);
        }
        catch(Exception ex) when(ex is RenderBudgetException or InvalidOperationException or ArgumentException)
        { return Plain(source); }
    }
    private static MarkdownResult Plain(string source)=>new("<div class=\"plain-text\">"+WebUtility.HtmlEncode(source)+"</div>",true);
    private sealed class RenderBudgetException : Exception;
}
