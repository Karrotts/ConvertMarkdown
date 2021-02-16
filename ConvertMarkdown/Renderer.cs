﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ConvertMarkdown
{
    public static class Renderer
    {
        public static string Heading(int size, string text)
        {
            switch(size)
            {
                case 1: return $"<h1>{text}</h1>";
                case 2: return $"<h2>{text}</h2>";
                case 3: return $"<h3>{text}</h3>";
                case 4: return $"<h4>{text}</h4>";
                case 5: return $"<h5>{text}</h5>";
                case 6: return $"<h6>{text}</h6>";
                default: return $"<h6>{text}</h6>";
            }
        }

        public static string Paragraph(string text)
        {
            return $"<p>{text}</p>";
        }

        public static string LineBreak()
        {
            return "<br>";
        }

        public static string Bold(string text)
        {
            return $"<strong>{text}</strong>";
        }

        public static string Italic(string text)
        {
            return $"<em>{text}</em>";
        }

        public static string BoldItalic(string text)
        {
            return $"<strong><em>{text}</em></strong>";
        }

        public static string BlockQuote(string text)
        {
            return $"<blockquote>{text}</blockquote>";
        }

        public static string OrderList(string[] text)
        {
            string output = "<ol>";
            foreach(string item in text)
            {
                output += $"<li>{item}</li>";
            }
            output += "</ol>";
            return output;
        }

        public static string UnorderedList(string[] text)
        {
            string output = "<ul>";
            foreach (string item in text)
            {
                output += $"<li>{item}</li>";
            }
            output += "</ul>";
            return output;
        }

        public static string Code(string text)
        {
            return $"<code>{text}</code>";
        }

        public static string Image(string src, string text)
        {
            return $"<img src=\"{src}\" alt=\"{text}\">";
        }
    }
}
