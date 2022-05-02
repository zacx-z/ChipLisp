using System.Collections.Generic;

namespace NelaSystem.ChipLisp {
    public class Env {
        public List<CellObj> vars;
        public Env up;

        public Env(List<CellObj> vars, Env up) {
            this.vars = vars ?? new List<CellObj>();
            this.up = up;
        }

        public CellObj Find(SymObj sym) {
            for (var p = this; p != null; p = p.up) {
                foreach (var bind in p.vars) {
                    if (sym == bind.car)
                        return bind;
                }
            }

            return null;
        }

        public void AddVariable(SymObj sym, Obj val) {
            vars.Add(new CellObj(sym, val));
        }
    }
}