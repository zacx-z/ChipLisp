using System;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Nela.ChipLisp {
    public class Lexer {
        private const string symbolChars = "~!@#$%^&*-_=+:/?<>";

        private VM vm;
        public char head => _head;
        private char _head;
        public TextReader reader { get; }
        private int sourceLinePos = 1;
        private int sourceCharPos = 0;

        public Lexer(VM vm, TextReader reader) {
            this.vm = vm;
            this.reader = reader;
            Next();
        }

        public (int, int) GetCurrentSourcePos() {
            return (sourceLinePos, sourceCharPos);
        }

        public Obj ReadObj() {
            var p = (sourceLinePos, sourceCharPos);
            var o = Read();
            SkipWhiteSpacesToNext();
            o.sourcePos = p;
            return o;
        }

        private Obj Read() {
            switch (head) {
            case '\"':
                return ReadString();
            case '-':
                if (reader.PeekChar(out var ch) && char.IsDigit(ch)) {
                    NextChar();
                    return ReadNumber(true);
                }

                break;
            case '.':
                if (reader.PeekChar(out var c) && char.IsDigit(c)) {
                    NextChar();
                    return new ValueObj<float>(ReadDecimal());
                }

                break;
            }

            if (char.IsDigit(head))
                return ReadNumber();
            if (char.IsLetter(head) || symbolChars.IndexOf(head) != -1)
                return ReadSymbol();

            throw new LexerException(this, $"Unexpected character encountered: {head}");
        }

        private bool NextChar() {
            var ret = reader.ReadChar(out _head);
            if (!ret) {
                return false;
            }

            sourceCharPos++;
            return ret;
        }

        public bool Next() {
            if (!NextChar()) return false;
            return SkipWhiteSpacesToNext();
        }

        public void Consume(char c) {
            if (c != head)
                throw new Exception($"Expected {c} but got {head}(ANSI {(int)head})");
            Next();
        }

        private bool SkipWhiteSpacesToNext() {
            while (true) {
                switch (head) {
                case '\n':
                    OnNextLine();
                    break;
                case ';':
                    SkipLine();
                    break;
                default:
                    if (!char.IsWhiteSpace(head))
                        return true;
                    break;
                }

                if (!NextChar()) return false;
            }
        }

        public void SkipLine() {
            if (reader.ReadLine() == null) {
                _head = '\0';
                return;
            }
            OnNextLine();
        }

        public void OnNextLine() {
            sourceLinePos++;
            sourceCharPos = 0;
        }

        public T ReadAs<T>(T o) where T : Obj {
            o.sourcePos = (sourceLinePos, sourceCharPos);
            NextChar();
            return o;
        }

        private Obj ReadNumber(bool negative = false) {
            int val = head - '0';
            while (NextChar()) {
                if (char.IsDigit(head)) {
                    val = val * 10 + (head - '0');
                } else if (head == '.') {
                    NextChar();
                    var fVal = val + ReadDecimal();
                    if (negative) fVal = -fVal;
                    return new ValueObj<float>(fVal);
                }
                else break;
            }

            if (char.IsLetter(head) || symbolChars.IndexOf(head) != -1)
                throw new LexerException(this, $"Unexpected character {head}");

            if (negative) val = -val;

            return new ValueObj<int>(val);
        }

        private float ReadDecimal() {
            if (!char.IsDigit(head)) return 0;
            float val = 0;
            float m = 0.1f;
            do {
                val += (head - '0') * m;
                m /= 10;
            } while (NextChar() && char.IsDigit(head));

            if (char.IsLetter(head) || symbolChars.IndexOf(head) != -1)
                throw new LexerException(this, $"Unexpected character {head}");

            return val;
        }

        private ValueObj<string> ReadString() {
            var str = new StringBuilder();
            bool escaping = false;
            while (NextChar() && (head != '\"' || escaping)) {
                str.Append(head);
                if (!escaping && head == '\\') {
                    escaping = true;
                } else {
                    escaping = false;
                }
            }

            if (head == '\"') {
                NextChar();
                return new ValueObj<string>(Regex.Unescape(str.ToString()));
            }

            throw new LexerException(this, "Unexpected end of file.");
        }

        private SymObj ReadSymbol() {
            var str = new StringBuilder();
            str.Append(head);
            while (NextChar()) {
                if (!(char.IsLetterOrDigit(head) || symbolChars.IndexOf(head) != -1)) break;
                str.Append(head);
            }

            return vm.Intern(str.ToString());
        }
    }
}