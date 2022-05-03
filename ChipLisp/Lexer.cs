using System.Text;
using System.IO;

namespace NelaSystem.ChipLisp {
    public class Lexer {
        private const string symbolChars = "~!@#$%^&*-_=+:/?<>";

        private VM vm;
        public char head { get; private set; }
        public bool isEnd { get; private set; } = false;
        public TextReader reader { get; }
        private int sourceLinePos = 1;
        private int sourceCharPos = 0;

        public Lexer(VM vm, TextReader reader) {
            this.vm = vm;
            this.reader = reader;
            NextChar();
        }

        public (int, int) GetCurrentSourcePos() {
            return (sourceLinePos, sourceCharPos);
        }

        public Obj ReadObj() {
            var p = (sourceLinePos, sourceCharPos);
            var o = Read();
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
                    return new NativeObj<float>(ReadDecimal());
                }

                break;
            }

            if (char.IsDigit(head))
                return ReadNumber();
            if (char.IsLetter(head) || symbolChars.IndexOf(head) != -1)
                return ReadSymbol();

            throw new LexerException(this, $"Unexpected character encountered: {head}");
        }

        public bool NextChar() {
            var ret = reader.ReadChar(out var c);
            head = c;
            if (!ret) isEnd = true;
            else {
                sourceCharPos++;
            }
            return ret;
        }

        public void SkipLine() {
            if (reader.ReadLine() == null) {
                isEnd = true;
                return;
            }
            OnNextLine();
            NextChar();
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
                    return new NativeObj<float>(fVal);
                }
                else break;
            }

            if (char.IsLetter(head) || symbolChars.IndexOf(head) != -1)
                throw new LexerException(this, $"Unexpected character {head}");

            if (negative) val = -val;

            return new NativeObj<int>(val);
        }

        private float ReadDecimal() {
            if (!char.IsDigit(head)) return 0;
            float val = 0;
            do {
                val += head - '0';
                val /= 10;
            } while (NextChar() && char.IsDigit(head));

            if (char.IsLetter(head) || symbolChars.IndexOf(head) != -1)
                throw new LexerException(this, $"Unexpected character {head}");

            return val;
        }

        private NativeObj<string> ReadString() {
            var str = new StringBuilder();
            while (NextChar() && head != '\"') {
                str.Append(head);
            }

            if (head == '\"') {
                NextChar();
                return new NativeObj<string>(str.ToString());
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