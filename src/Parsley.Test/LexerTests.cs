﻿using System.Collections.Generic;
using Should;
using Xunit;

namespace Parsley
{
    public class LexerTests
    {
        private readonly TokenKind lower;
        private readonly TokenKind upper;

        public LexerTests()
        {
            lower = new Pattern("Lowercase", @"[a-z]+");
            upper = new Pattern("Uppercase", @"[A-Z]+");
        }

        [Fact]
        public void ProvidesCurrentToken()
        {
            var lexer = new Lexer(new Text("ABCdef"), upper);
            lexer.CurrentToken.ShouldBe(upper, "ABC", 1, 1);
        }

        [Fact]
        public void AdvancesToTheNextToken()
        {
            var lexer = new Lexer(new Text("ABCdef"), upper, lower);
            lexer.Advance().CurrentToken.ShouldBe(lower, "def", 1, 4);
        }

        [Fact]
        public void ProvidesTokenAtEndOfInput()
        {
            var lexer = new Lexer(new Text(""));
            lexer.CurrentToken.ShouldBe(Lexer.EndOfInput, "", 1, 1);
        }

        [Fact]
        public void TryingToAdvanceBeyondEndOfInputResultsInNoMovement()
        {
            var lexer = new Lexer(new Text(""));
            lexer.ShouldBeSameAs(lexer.Advance());
        }

        [Fact]
        public void UsesPrioritizedTokenMatchersToGetCurrentToken()
        {
            var lexer = new Lexer(new Text("ABCdefGHI"), lower, upper);
            lexer.CurrentToken.ShouldBe(upper, "ABC", 1, 1);
            lexer.Advance().CurrentToken.ShouldBe(lower, "def", 1, 4);
            lexer.Advance().Advance().CurrentToken.ShouldBe(upper, "GHI", 1, 7);
            lexer.Advance().Advance().Advance().CurrentToken.ShouldBe(Lexer.EndOfInput, "", 1, 10);
        }

        [Fact]
        public void CanBeEnumerated()
        {
            var tokens = ToArray(new Lexer(new Text("ABCdefGHIjkl"), lower, upper));
            tokens.Length.ShouldEqual(5);
            tokens[0].ShouldBe(upper, "ABC", 1, 1);
            tokens[1].ShouldBe(lower, "def", 1, 4);
            tokens[2].ShouldBe(upper, "GHI", 1, 7);
            tokens[3].ShouldBe(lower, "jkl", 1, 10);
            tokens[4].ShouldBe(Lexer.EndOfInput, "", 1, 13);
        }

        [Fact]
        public void ProvidesTokenAtUnrecognizedInput()
        {
            var tokens = ToArray(new Lexer(new Text("ABC!def"), upper, lower));
            tokens.Length.ShouldEqual(3);
            tokens[0].ShouldBe(upper, "ABC", 1, 1);
            tokens[1].ShouldBe(Lexer.Unknown, "!def", 1, 4);
            tokens[2].ShouldBe(Lexer.EndOfInput, "", 1, 8);
        }

        [Fact]
        public void SkipsPastSkippableTokens()
        {
            var space = new Pattern("Space", @"\s", skippable: true);

            var tokens = ToArray(new Lexer(new Text(" "), lower, upper, space));
            tokens.Length.ShouldEqual(1);
            tokens[0].ShouldBe(Lexer.EndOfInput, "", 1, 2);

            tokens = ToArray(new Lexer(new Text(" ABC  def   GHI    jkl"), lower, upper, space));
            tokens.Length.ShouldEqual(5);
            tokens[0].ShouldBe(upper, "ABC", 1, 2);
            tokens[1].ShouldBe(lower, "def", 1, 7);
            tokens[2].ShouldBe(upper, "GHI", 1, 13);
            tokens[3].ShouldBe(lower, "jkl", 1, 20);
            tokens[4].ShouldBe(Lexer.EndOfInput, "", 1, 23);
        }

        private Token[] ToArray(Lexer lexer)
        {
            var tokens = new List<Token> { lexer.CurrentToken };

            while (lexer.CurrentToken.Kind != Lexer.EndOfInput)
            {
                lexer = lexer.Advance();
                tokens.Add(lexer.CurrentToken);
            }

            return tokens.ToArray();
        }
    }
}