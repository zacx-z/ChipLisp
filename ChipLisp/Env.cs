using System.Collections.Generic;

namespace Nela.ChipLisp {
    public class Env {
        public List<CellObj> vars { get; }
        public Env up { get; }

        public Env(List<CellObj> vars, Env up) {
            this.vars = vars ?? new List<CellObj>();
            this.up = up;
        }

        public static Env FromMapping(List<CellObj> vars, Env up = null) {
            return new Env(new List<CellObj>(vars), up);
        }

        public CellObj Find(SymObj sym) {
            for (var p = this; p != null; p = p.up) {
                foreach (var bind in p.vars) {
                    if (sym == bind.car)
                        return bind;
                }
            }

            throw new SymbolNotFoundException(sym, this);
        }

        public void AddVariable(SymObj sym, Obj val) {
            vars.Add(new CellObj(sym, val));
        }
    }
}