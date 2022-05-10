using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nela.ChipLisp.Libs {
    public static class ClrLib {
        private static Assembly[] _assemblies;
        private static Assembly[] assemblies => _assemblies ?? (_assemblies = AppDomain.CurrentDomain.GetAssemblies());

        public static void Load(State state) {
            state.AddFunction("clr-assemblies", Prim_GetAssemblies);
            state.AddFunction("clr-all-types", Prim_GetTypes);
            state.AddFunction("clr-type-from-full-name", Prim_TypeFromFullName);
            state.AddFunction("clr-cast", Prim_Cast);
            state.AddFunction("clr-is", Prim_Is);
            state.AddFunction("clr-new", Prim_New);
            state.AddFunction("clr-get-type", Prim_GetType);
            state.AddPrimitive("clr-call", Prim_Call);
            state.AddPrimitive("clr-call-member", Prim_CallMember);
            state.AddFunction("clr-methods", Prim_Methods);
            state.AddFunction("clr-static-methods", Prim_StaticMethods);
        }

        private static ValueObj CreateDynamicValueObj(object o, Type type) {
            return (ValueObj)Activator.CreateInstance(typeof(ValueObj<>).MakeGenericType(type), o);
        }

        private static IEnumerable<object> GetRestAsParams(VM vm, ListEnumerator enumerator) {
            while (enumerator.GetNext(out var p)) {
                yield return vm.ExpectValue<object>(p);
            }
        }

        private static Obj Prim_GetAssemblies(VM vm, Env env, Obj args) {
            vm.ExpectList0(args);
            Obj head = Obj.nil;
            foreach (var ass in assemblies) {
                head = VM.Cons(new ValueObj<Assembly>(ass), head);
            }

            return VM.Reverse(head);
        }

        private static Obj Prim_GetTypes(VM vm, Env env, Obj args) {
            var ass = vm.ExpectValue<Assembly>(vm.ExpectList1(args));
            var types = ass.GetTypes();
            Obj head = Obj.nil;
            foreach (var t in types) {
                head = VM.Cons(new ValueObj<Type>(t), head);
            }

            return VM.Reverse(head);
        }

        private static Obj Prim_TypeFromFullName(VM vm, Env env, Obj args) {
            var name = vm.ExpectValue<string>(vm.ExpectList1(args));
            foreach (var ass in assemblies) {
                var t = ass.GetType(name);
                if (t != null)
                    return new ValueObj<Type>(t);
            }

            return Obj.nil;
        }

        private static Obj Prim_Cast(VM vm, Env env, Obj args) {
            var (typeObj, obj) = vm.ExpectList2(args);
            var o = vm.ExpectValue<object>(obj);
            var type = vm.ExpectValue<Type>(typeObj);
            if (type.IsInstanceOfType(o))
                return CreateDynamicValueObj(o, type);
            return Obj.nil;
        }

        private static Obj Prim_Is(VM vm, Env env, Obj args) {
            var (typeObj, obj) = vm.ExpectList2(args);
            var o = vm.ExpectValue<object>(obj);
            var type = vm.ExpectValue<Type>(typeObj);
            if (type.IsInstanceOfType(o))
                return TrueObj.t;
            return Obj.nil;
        }

        private static Obj Prim_New(VM vm, Env env, Obj args) {
            var enumerator = args.GetListEnumerator();
            enumerator.GetNext(out var oType);
            var type = vm.ExpectValue<Type>(oType);

            var res = Activator.CreateInstance(type, GetRestAsParams(vm, enumerator).ToArray());
            return CreateDynamicValueObj(res, type);
        }

        private static Obj Prim_GetType(VM vm, Env env, Obj args) {
            var obj = vm.ExpectList1(args);
            return new ValueObj<Type>(vm.ExpectValue<object>(obj).GetType());
        }

        // clr-call type 'method self arg1 arg2 ...
        private static Obj Prim_Call(VM vm, Env env, Obj args) {
            var enumerator = args.GetListEnumerator();
            enumerator.GetNext(out var oType);
            var type = vm.ExpectValue<Type>(vm.Eval(env, oType));

            enumerator.GetNext(out var oMethod);
            var method = vm.Expect<SymObj>(oMethod).name;

            object self = null;
            if (enumerator.GetNext(out var oSelf)) {
                self = vm.ExpectValue<object>(vm.Eval(env, oSelf));
            }

            var callArgs = new List<object>();
            while (enumerator.GetNext(out var p)) {
                callArgs.Add(vm.ExpectValue<object>(vm.Eval(env, p)));
            }

            var res = type.GetMethod(method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Invoke(self, callArgs.ToArray());

            return res == null ? Obj.nil : (Obj)CreateDynamicValueObj(res, res.GetType());
        }

        // clr-call-member self 'method arg1 arg2 ...
        private static Obj Prim_CallMember(VM vm, Env env, Obj args) {
            var enumerator = args.GetListEnumerator();

            enumerator.GetNext(out var oSelf);
            object self = vm.ExpectValue<object>(vm.Eval(env, oSelf));

            enumerator.GetNext(out var oMethod);
            var method = vm.Expect<SymObj>(oMethod).name;

            var callArgs = new List<object>();
            while (enumerator.GetNext(out var p)) {
                callArgs.Add(vm.ExpectValue<object>(vm.Eval(env, p)));
            }

            var res = self.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Invoke(self, callArgs.ToArray());

            return res == null ? Obj.nil : (Obj)CreateDynamicValueObj(res, res.GetType());
        }

        private static Obj Prim_Methods(VM vm, Env env, Obj args) {
            var typeObj = vm.ExpectList1(args);
            var type = vm.ExpectValue<Type>(typeObj);
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            Obj head = Obj.nil;
            foreach (var m in methods) {
                head = VM.Cons(VM.Intern(m.Name), head);
            }

            return VM.Reverse(head);
        }

        private static Obj Prim_StaticMethods(VM vm, Env env, Obj args) {
            var typeObj = vm.ExpectList1(args);
            var type = vm.ExpectValue<Type>(typeObj);
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
            Obj head = Obj.nil;
            foreach (var m in methods) {
                head = VM.Cons(VM.Intern(m.Name), head);
            }

            return VM.Reverse(head);
        }
    }
}