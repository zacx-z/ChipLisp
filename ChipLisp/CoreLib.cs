namespace NelaSystem.ChipLisp {
    public static class CoreLib {
        public static void Load(State state) {
            state.AddVariable("t", TrueObj.t);
            state.AddVariable("quote", new PrimObj(Quote));
        }

        private static Obj Quote(VM vm, Env env, Obj list) {
            if (list.GetListLength() != 1) {
                vm.Error("malformed quote");
            }
            return ((CellObj)list).car;
        }
    }
}