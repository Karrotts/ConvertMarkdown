﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ConvertMarkdown
{
    public enum TokenType
    {
        Header,
        Paragraph,
        Bold,
        Italic,
        BoldItalic,
        BlockQuote,
        Link,
        OrderedList,
        UnorderedList
    }
    public class Tokenizer
    {
        private List<TokenMatcher> tokenMatchers;
        private List<TokenMatcher> specialTokenMatchers;
        private Stack<TokenType> foundSpecialTokens;

        private byte tabIndex;

        public Tokenizer()
        {
            tokenMatchers = new List<TokenMatcher>();
            specialTokenMatchers = new List<TokenMatcher>();
            foundSpecialTokens = new Stack<TokenType>();

            tabIndex = 0;

            tokenMatchers.Add(new TokenMatcher(TokenType.Header, "^#+ (.+)"));
            tokenMatchers.Add(new TokenMatcher(TokenType.BoldItalic, "\\*\\*\\*(.+?)\\*\\*\\*"));
            tokenMatchers.Add(new TokenMatcher(TokenType.Bold, "\\*\\*(.+?)\\*\\*"));
            tokenMatchers.Add(new TokenMatcher(TokenType.Italic, "\\*(.+?)\\*"));
            tokenMatchers.Add(new TokenMatcher(TokenType.Link, "\\[(.+?)\\]\\((.+?)\\)"));
            specialTokenMatchers.Add(new TokenMatcher(TokenType.UnorderedList, "^\\* (.+)"));
            specialTokenMatchers.Add(new TokenMatcher(TokenType.OrderedList, "^[0-9]+\\. (.+)"));
            specialTokenMatchers.Add(new TokenMatcher(TokenType.BlockQuote, "^> (.+)"));
            specialTokenMatchers.Add(new TokenMatcher(TokenType.BlockQuote, "^>> (.+)"));
        }

        public string Tokenize(string text, List<string> htmlLines)
        {
            SpecialTokenize(ref text, htmlLines);
            SimpleTokenize(ref text);
            return text;
        }

        public void SimpleTokenize(ref string line)
        {
            bool headerFound = false;
            foreach (TokenMatcher matcher in tokenMatchers)
            {
                TokenMatch match = matcher.Match(line);
                while (match.IsMatch)
                {
                    string replacement = "";
                    switch (match.TokenType)
                    {
                        case TokenType.Header:
                            int tokenCount = line.Length - line.Replace("#", "").Length;
                            replacement = Renderer.Heading(tokenCount, match.Value);
                            headerFound = true;
                            break;
                        case TokenType.Italic:
                            replacement = Renderer.Italic(match.Value);
                            break;
                        case TokenType.Bold:
                            replacement = Renderer.Bold(match.Value);
                            break;
                        case TokenType.BoldItalic:
                            replacement = Renderer.BoldItalic(match.Value);
                            break;
                        case TokenType.Link:
                            replacement = Renderer.Link(match.BaseMatch.Groups[1].Value, match.BaseMatch.Groups[2].Value);
                            break;
                        default:
                            break;
                    }
                    line = Builder.RepaceInString(line, replacement, match.StartIndex, match.EndIndex);
                    match = matcher.Match(line);
                }
            }

            if(!headerFound && !string.IsNullOrWhiteSpace(line))
            {
                line = Builder.RepaceInString(line, Renderer.Paragraph(line), 0, line.Length - 1);
            }
        }

        public void SpecialTokenize(ref string line, List<string> htmlLines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                htmlLines.Add("<br>");
                return;
            }

            bool itemFound = false;
            bool tabFound = line.IndexOf("\t") == 0;

            byte newTabIndex = CurrentTab(line);

            line = tabFound ? line.Replace("\t", "") : line;

            foreach (TokenMatcher matcher in specialTokenMatchers)
            {
                TokenMatch match = matcher.Match(line);
                if (match.IsMatch)
                {
                    itemFound = true;
                    switch (match.TokenType)
                    {
                        case TokenType.BlockQuote:
                            line = Builder.RepaceInString(line, "", 0, 1);

                            if (foundSpecialTokens.Count == 0 || foundSpecialTokens.Peek() != TokenType.BlockQuote)
                            {
                                htmlLines.Add("<blockquote>");
                                foundSpecialTokens.Push(TokenType.BlockQuote);
                            }
                            break;
                        case TokenType.UnorderedList:
                            // Check if the previous token was an unordered list and pop it from the stack
                            if (foundSpecialTokens.Count > 0
                            && foundSpecialTokens.Peek() == TokenType.OrderedList)
                            {
                                htmlLines.Add("</ol>");
                                foundSpecialTokens.Pop();
                            }

                            // Add new element if there is a tab or first time seeing the match
                            if (foundSpecialTokens.Count == 0 
                            || foundSpecialTokens.Peek() != TokenType.UnorderedList 
                            || tabFound && newTabIndex > tabIndex && newTabIndex - tabIndex == 1)
                            {
                                htmlLines.Add("<ul>");
                                foundSpecialTokens.Push(TokenType.UnorderedList);
                                tabIndex = newTabIndex;
                            }

                            // Close child element if untabbed
                            if (newTabIndex < tabIndex)
                            {
                                while(tabIndex - newTabIndex > 0)
                                {
                                    if(foundSpecialTokens.Peek() == TokenType.UnorderedList)
                                    {
                                        htmlLines.Add("</ul>");
                                        foundSpecialTokens.Pop();
                                        tabIndex--;
                                    }
                                }
                            }

                            // Insert list item into line
                            line = Builder.RepaceInString(line,
                                                          Renderer.ListItem(match.Value),
                                                          match.StartIndex,
                                                          match.EndIndex);
                            break;
                        case TokenType.OrderedList:
                            // Check if the previous token was an unordered list and pop it from the stack
                            if (foundSpecialTokens.Count > 0
                            && foundSpecialTokens.Peek() == TokenType.UnorderedList)
                            {
                                htmlLines.Add("</ul>");
                                foundSpecialTokens.Pop();
                            }

                            // Add new element if there is a tab or first time seeing the match
                            if (foundSpecialTokens.Count == 0
                            || foundSpecialTokens.Peek() != TokenType.OrderedList
                            || tabFound && newTabIndex > tabIndex && newTabIndex - tabIndex == 1)
                            {
                                htmlLines.Add("<ol>");
                                foundSpecialTokens.Push(TokenType.OrderedList);
                                tabIndex = newTabIndex;
                            }

                            // Close child element if untabbed
                            else if (newTabIndex < tabIndex)
                            {
                                while (tabIndex - newTabIndex > 0)
                                {
                                    if (foundSpecialTokens.Peek() == TokenType.OrderedList)
                                    {
                                        htmlLines.Add("</ol>");
                                        foundSpecialTokens.Pop();
                                        tabIndex--;
                                    }
                                }
                            }

                            // Insert list item into line
                            line = Builder.RepaceInString(line,
                                                          Renderer.ListItem(match.Value),
                                                          match.StartIndex,
                                                          match.EndIndex);
                            break;
                        default:
                            break;
                    }
                }
            }

            if (!itemFound)
            {
                htmlLines.Add(Close());
            }
        }

        public string Close()
        {
            tabIndex = 0;
            string output = "";
            while(foundSpecialTokens.Count > 0)
            {
                switch(foundSpecialTokens.Peek())
                {
                    case TokenType.BlockQuote:
                        output += "</blockquote>";
                        foundSpecialTokens.Pop();
                        break;
                    case TokenType.UnorderedList:
                        output += "</ul>";
                        foundSpecialTokens.Pop();
                        break;
                    case TokenType.OrderedList:
                        output += "</ol>";
                        foundSpecialTokens.Pop();
                        break;
                    default:
                        break;
                }
            }
            return output;
        }

        public byte CurrentTab(string line)
        {
            byte count = 0;
            while (line.IndexOf("\t", count) >= 0)
                count++;
            return count; 
        }
    }

    public class TokenMatcher
    {
        private Regex regex;
        private TokenType type;

        public TokenMatcher(TokenType type, string regexPattern)
        {
            this.regex = new Regex(regexPattern);
            this.type = type;
        }

        public TokenMatch Match(string inputString)
        {
            var match = regex.Match(inputString);
            if (match.Success)
            {
                return new TokenMatch()
                {
                    IsMatch = true,
                    TokenType = type,
                    BaseMatch = match,
                    Value = match.Groups[1].Value,
                    StartIndex = match.Index,
                    EndIndex = match.Index + match.Length - 1
                };
            }
            else
            {
                return new TokenMatch() { IsMatch = false };
            }

        }
    }

    public class TokenMatch
    {
        public bool IsMatch { get; set; }
        public TokenType TokenType { get; set; }
        public Match BaseMatch { get; set; }
        public string Value { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
    }
}
