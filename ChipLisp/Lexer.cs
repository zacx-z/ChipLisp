using System.Text;
using System.IO;

namespace NelaSystem.ChipLisp {
    public class Lexer {
        private const string symbolChars = "~!@#$%^&*-_=+:/?<>";

        private VM vm;
        public char head { get; private set; }
        public bool isEnd { get; private set; } = false;
        private TextReader reader;

        public Lexer(VM vm, TextReader reader) {
            this.vm = vm;
            this.reader = reader;
            ReadNext();
        }

        public Obj Read() {
            if (head == '-') {
                if (reader.PeekChar(out var ch)&& char.IsDigit(ch)) {
                    ReadNext();
                    return ReadNumber(true);
                }
            }

            if (char.IsDigit(head))
                return ReadNumber();
            if (char.IsLetter(head) || symbolChars.IndexOf(head) != -1)
                return ReadSymbol();

            throw new InvalidDataException($"Don't know how to handle {head}");
        }

        public bool ReadNext() {
            var ret = reader.ReadChar(out var c);
            head = c;
            if (!ret) isEnd = true;
            return ret;
        }

        public void SkipLine() {
            if (reader.ReadLine() == null) {
                isEnd = true;
                return;
            }
            ReadNext();
        }

        public T ReadAs<T>(T o) where T : Obj {
            ReadNext();
            return o;
        }

        private Obj ReadNumber(bool negative = false) {
            int val = head - '0';
            while (ReadNext()) {
                if (char.IsDigit(head)) {
                    val = val * 10 + (head - '0');
                } else if (head == '.') {
                    ReadNext();
                    var fVal = val + ReadDecimal();
                    if (negative) fVal = -fVal;
                    return new NativeObj<float>(fVal);
                }
                else break;
            }

            if (negative) val = -val;

            return new NativeObj<int>(val);
        }

        private float ReadDecimal() {
            if (!char.IsDigit(head)) return 0;
            float val = 0;
            do {
                val += head - '0';
                val /= 10;
            } while (ReadNext() && char.IsDigit(head));

            return val;
        }

        private SymObj ReadSymbol() {
            var str = new StringBuilder();
            str.Append(head);
            while (ReadNext()) {
                if (!(char.IsLetterOrDigit(head) || symbolChars.IndexOf(head) != -1)) break;
                str.Append(head);
            }

            return vm.Intern(str.ToString());
        }
    }
}