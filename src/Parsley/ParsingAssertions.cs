﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Parsley
{
    public static class ParsingAssertions
    {
        public static void ShouldYieldTokens(this TokenStream tokens, TokenKind expectedKind, params string[] expectedLiterals)
        {
            foreach (var expectedLiteral in expectedLiterals)
            {
                tokens.Current.ShouldBe(expectedKind, expectedLiteral);
                tokens = tokens.Advance();
            }

            AssertEqual(TokenKind.EndOfInput, tokens.Current.Kind);
        }

        public static void ShouldYieldTokens(this TokenStream tokens, params string[] expectedLiterals)
        {
            foreach (var expectedLiteral in expectedLiterals)
            {
                AssertTokenLiteralsEqual(expectedLiteral, tokens.Current.Literal);
                AssertNotEqual(TokenKind.Unknown, tokens.Current.Kind);
                tokens = tokens.Advance();
            }

            AssertEqual(TokenKind.EndOfInput, tokens.Current.Kind);
        }

        public static void ShouldBe(this Token actual, TokenKind expectedKind, string expectedLiteral, int expectedLine, int expectedColumn)
        {
            actual.ShouldBe(expectedKind, expectedLiteral);

            var expectedPosition = new Position(expectedLine, expectedColumn);
            if (actual.Position != expectedPosition)
                throw new AssertionException("token at position " + expectedPosition,
                                             "token at position " + actual.Position);
        }

        public static void ShouldBe(this Token actual, TokenKind expectedKind, string expectedLiteral)
        {
            AssertEqual(expectedKind, actual.Kind);
            AssertTokenLiteralsEqual(expectedLiteral, actual.Literal);
        }

        public static Reply<T> FailsToParse<T>(this Parser<T> parser, TokenStream tokens)
        {
            return parser.Parse(tokens).Fails();
        }

        private static Reply<T> Fails<T>(this Reply<T> reply)
        {
            if (reply.Success)
                throw new AssertionException("parser failure", "parser completed successfully");

            return reply;
        }

        public static Reply<T> WithMessage<T>(this Reply<T> reply, string expectedMessage)
        {
            var position = reply.UnparsedTokens.Position;
            var actual = position + ": " + reply.ErrorMessages;
            
            if (actual != expectedMessage)
                throw new AssertionException(string.Format("message at {0}", expectedMessage),
                                             string.Format("message at {0}", actual));

            return reply;
        }

        public static Reply<T> WithNoMessage<T>(this Reply<T> reply)
        {
            if (reply.ErrorMessages != ErrorMessageList.Empty)
                throw new AssertionException("no error message", reply.ErrorMessages);

            return reply;
        }

        public static Reply<T> PartiallyParses<T>(this Parser<T> parser, TokenStream tokens)
        {
            return parser.Parse(tokens).Succeeds();
        }

        public static Reply<T> Parses<T>(this Parser<T> parser, TokenStream tokens)
        {
            return parser.Parse(tokens).Succeeds().AtEndOfInput();
        }

        private static Reply<T> Succeeds<T>(this Reply<T> reply)
        {
            if (!reply.Success)
                throw new AssertionException(reply.ErrorMessages.ToString(), "parser success", "parser failed");

            return reply;
        }

        //TODO: Suspicious overlap with "IntoTokens".
        public static Reply<T> LeavingUnparsedTokens<T>(this Reply<T> reply, params string[] expectedLiterals)
        {
            var actualLiterals = reply.UnparsedTokens.Where(x => x.Kind != TokenKind.EndOfInput).Select(x => x.Literal).ToArray();

            Action raiseError = () =>
            {
                throw new AssertionException("Parse resulted in unexpected remaining unparsed tokens.",
                                             String.Join(", ", expectedLiterals),
                                             String.Join(", ", actualLiterals));
            };

            if (actualLiterals.Length != expectedLiterals.Length)
                raiseError();

            for (int i = 0; i < actualLiterals.Length; i++)
                if (actualLiterals[i] != expectedLiterals[i])
                    raiseError();

            return reply;
        }

        public static Reply<T> AtEndOfInput<T>(this Reply<T> reply)
        {
            //TODO: Can we just do the final return instead?
            var nextTokenKind = reply.UnparsedTokens.Current.Kind;
            AssertEqual(TokenKind.EndOfInput, nextTokenKind);
            return reply.LeavingUnparsedTokens(new string[] {});
        }

        public static Reply<T> IntoValue<T>(this Reply<T> reply, T expected)
        {
            if (!Equals(expected, reply.Value))
                throw new AssertionException(string.Format("parsed value: {0}", expected),
                                             string.Format("parsed value: {0}", reply.Value));

            return reply;
        }

        public static Reply<T> IntoValue<T>(this Reply<T> reply, Action<T> assertParsedValue)
        {
            assertParsedValue(reply.Value);

            return reply;
        }

        public static Reply<Token> IntoToken(this Reply<Token> reply, TokenKind expectedKind, string expectedLiteral)
        {
            reply.Value.ShouldBe(expectedKind, expectedLiteral);

            return reply;
        }

        public static Reply<Token> IntoToken(this Reply<Token> reply, string expectedLiteral)
        {
            AssertTokenLiteralsEqual(expectedLiteral, reply.Value.Literal);
            return reply;
        }

        public static Reply<IEnumerable<Token>> IntoTokens(this Reply<IEnumerable<Token>> reply, params string[] expectedLiterals)
        {
            var actualLiterals = reply.Value.Select(x => x.Literal).ToArray();

            Action raiseError = () =>
            {
                throw new AssertionException("Parse resulted in unexpected token literals.",
                                             String.Join(", ", expectedLiterals),
                                             String.Join(", ", actualLiterals));
            };

            if (actualLiterals.Length != expectedLiterals.Length)
                raiseError();

            for (int i = 0; i < actualLiterals.Length; i++)
                if (actualLiterals[i] != expectedLiterals[i])
                    raiseError();

            return reply;
        }

        private static void AssertTokenLiteralsEqual(string expected, string actual)
        {
            if (actual != expected)
                throw new AssertionException(string.Format("token with literal \"{0}\"", expected),
                                             string.Format("token with literal \"{0}\"", actual));
        }

        private static void AssertEqual(TokenKind expected, TokenKind actual)
        {
            if (actual != expected)
                throw new AssertionException(string.Format("<{0}> token", expected),
                                             string.Format("<{0}> token", actual));
        }

        private static void AssertNotEqual(TokenKind expected, TokenKind actual)
        {
            if (actual == expected)
                throw new AssertionException(string.Format("not <{0}> token", expected),
                                             string.Format("<{0}> token", actual));
        }
    }
}