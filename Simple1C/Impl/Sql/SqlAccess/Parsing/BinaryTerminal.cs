using System.Text.RegularExpressions;
using Irony.Parsing;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.SqlAccess.Parsing
{
    public class BinaryTerminal : Terminal
    {
        public BinaryTerminal(string name) : base(name)
        {
            AstConfig.NodeCreator = (context, node) =>
            {
                node.AstNode = new LiteralExpression
                {
                    Value = ByteArrayHelpers.FromHex(node.Token.ValueString),
                    SqlType = SqlType.ByteArray
                };
            };
        }

        private static readonly Regex binaryRegex =
            new Regex(@"\\x[a-f0-9]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public override Token TryMatch(ParsingContext context, ISourceStream source)
        {
            var match = binaryRegex.Match(source.Text, source.PreviewPosition);
            if (!match.Success || match.Index != source.PreviewPosition)
                return null;
            source.Position += 2;
            source.PreviewPosition += match.Length;
            return source.CreateToken(OutputTerminal);
        }
    }
}