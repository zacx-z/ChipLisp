using System.Diagnostics;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Nela.ChipLisp {
    public interface ILexer {
        char head { get; }
        Obj Read();
        void Next();
        bool PeekChar(out char ch);
        (int, int) GetCurrentSourcePos();
    }

    public class Lexer : ILexer {
        private const string symbolChars = "~!@#$%^&*-_=+:/?<>";

        public char head {
            get {
                if (headOnRequest) {
                    SkipWhiteSpacesToNext();
                    headOnRequest = false;
                }
                return _head;
            }
        }

        private char _head;
        private bool headOnRequest = false;

        public TextReader reader { get; }
        private int sourceLinePos = 1;
        private int sourceCharPos = 0;

        public Lexer(TextReader reader) {
            this.reader = reader;
            Next();
        }

        public (int, int) GetCurrentSourcePos() {
            return (sourceLinePos, sourceCharPos);
        }

        public bool PeekChar(out char ch) => reader.PeekChar(out ch);

        public Obj Read() {
            var p = (sourceLinePos, sourceCharPos);
            var o = ReadObj();
            Debug.Assert(!headOnRequest);
            headOnRequest = true;
            o.sourcePos = p;
            return o;
        }

        protected virtual Obj ReadObj() {
            switch (head) {
            case '\"':
                return ReadString();
            case '\\':
                return ReadChar();
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

        // use lazy evaluation to be compatible to REPL
        public void Next() {
            if (headOnRequest) {
                var _ = head;
            }

            NextChar();
            headOnRequest = true;
        }

        private bool SkipWhiteSpacesToNext() {
            while (true) {
                switch (_head) {
                case '\n':
                    OnNextLine();
                    break;
                case ';':
                    SkipLine();
                    break;
                default:
                    if (!char.IsWhiteSpace(_head))
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

        private ValueObj<char> ReadChar() {
            NextChar();
            var str = new StringBuilder();
            bool escaping = false;
            if (head != '\'')
                throw new LexerException(this, $"Expected {"'"} but got {head}");

            while (NextChar() && (head != '\'' || escaping)) {
                str.Append(head);
                if (!escaping && head == '\\') {
                    escaping = true;
                } else {
                    escaping = false;
                }
            }

            if (head == '\'') {
                NextChar();
                var s = Regex.Unescape(str.ToString());
                if (s.Length != 1)
                    throw new LexerException(this, $"Invalid char literal {str}");
                return new ValueObj<char>(s[0]);
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

            return VM.Intern(str.ToString());
        }
    }
}