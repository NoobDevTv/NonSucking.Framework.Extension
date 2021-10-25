using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NonSucking.Framework.Extension.Generators.Vavare
{
    internal class BlockHelper
    {
        static SyntaxToken openEmpty = SyntaxFactory.Token(default, SyntaxKind.OpenBraceToken, "", "", default);
        static SyntaxToken closeEmpty = SyntaxFactory.Token(default, SyntaxKind.CloseBraceToken, "", "", default);
        internal static StatementSyntax GetBlockWithoutBraces(IEnumerable<StatementSyntax> statements)
        {
            StatementSyntax statement;

            statement
                = SyntaxFactory.Block(openEmpty, SyntaxFactory.List(statements.ToArray()), closeEmpty);
            return statement;
        }
    }
}
