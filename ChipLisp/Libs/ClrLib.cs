using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Nela.ChipLisp.Libs {
    public static class ClrLib {
        private static Assembly[] _assemblies;
        private static Assembly[] assemblies => _assemblies ?? (_assemblies = AppDomain.CurrentDomain.GetAssemblies());
        private static SymObj namespacesSym = VM.Intern(".imported-ns");

        public static void Load(State state) {
            state.AddFunction("clr-assemblies", Prim_GetAssemblies);
            state.AddFunction("clr-all-types", Prim_GetTypes);
            state.AddFunction("clr-type-from-full-name", Prim_TypeFromFullName);
            state.AddFunction("clr-cast", Prim_Cast);
            state.AddFunction("clr-coerce", Prim_Coerce);
            state.AddFunction("clr-is", Prim_Is);
            state.AddFunction("clr-new", Prim_New);
            state.AddFunction("clr-get-types", Prim_GetType);
            state.AddFunction("clr-call", Prim_Call);
            state.AddFunction("clr-call-member", Prim_CallMember);
            state.AddFunction("clr-methods", Prim_Methods);
            state.AddFunction("clr-static-methods", Prim_StaticMethods);
            // with local using scope
            state.AddFunction("clr-using", Prim_Using);
            state.AddFunction("clr-type", Prim_Type);
            state.AddFunction("clr-types", Prim_Types);
        }

        private static ValueObj CreateDynamicValueObj(object o, Type type) {
            return (ValueObj)Activator.CreateInstance(typeof(ValueObj<>).MakeGenericType(type), o);
        }

        private static IEnumerable<object> GetRestAsParams(VM vm, ListEnumerator enumerator) {
            while (enumerator.GetNext(out var p)) {
                yield return vm.Expect<ValueObj>(p).untypedValue;
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

        // (clr-cast type o)
        private static Obj Prim_Cast(VM vm, Env env, Obj args) {
            var (typeObj, obj) = vm.ExpectList2(args);
            var o = vm.Expect<ValueObj>(obj);
            var type = vm.ExpectValue<Type>(typeObj);
            if (type.IsInstanceOfType(o.untypedValue))
                return CreateDynamicValueObj(o.untypedValue, type);
            return Obj.nil;
        }

        // (clr-coerce type o)
        private static Obj Prim_Coerce(VM vm, Env env, Obj args) {
            var (typeObj, obj) = vm.ExpectList2(args);
            var o = vm.Expect<ValueObj>(obj);
            var type = vm.ExpectValue<Type>(typeObj);

            object res;
            if (type.IsEnum) {
                res = Enum.ToObject(type, o.untypedValue);
            }
            else {
                res = Convert.ChangeType(o.untypedValue, type);
            }

            return CreateDynamicValueObj(res, type);
        }

        private static Obj Prim_Is(VM vm, Env env, Obj args) {
            var (typeObj, obj) = vm.ExpectList2(args);
            var o = vm.Expect<ValueObj>(obj).untypedValue;
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
            return new ValueObj<Type>(vm.Expect<ValueObj>(obj).type);
        }

        private static Obj Call(VM vm, Env env, Type type, object self, string method, ListEnumerator args, BindingFlags bindingFlags) {
            var callArgs = new List<object>();
            while (args.GetNext(out var p)) {
                callArgs.Add(vm.Expect<ValueObj>(p).untypedValue);
            }

            object res;
            if (!method.StartsWith("@")) {
                Console.WriteLine(callArgs[1]);
                res = type.GetMethod(method, bindingFlags, null, CallingConventions.Any | CallingConventions.HasThis, 
                        callArgs.Select(a => a.GetType()).ToArray(), null)
                    .Invoke(self, callArgs.ToArray());
            }
            else {
                if (!method.StartsWith("@:")) {
                    var property = type
                        .GetProperty(method.Substring(1), bindingFlags);
                    if (callArgs.Count == 0) res = property.GetValue(self);
                    else {
                        property.SetValue(self, callArgs[0]);
                        res = callArgs[0];
                    }
                } else {
                    var field = type
                        .GetField(method.Substring(2), bindingFlags);
                    if (callArgs.Count == 0) res = field.GetValue(self);
                    else {
                        field.SetValue(self, callArgs[0]);
                        res = callArgs[0];
                    }
                }
            }
            return res == null ? Obj.nil : (Obj) CreateDynamicValueObj(res, res.GetType());
        }

        // clr-call type 'method self arg1 arg2 ...
        // method could be @property or @:field
        private static Obj Prim_Call(VM vm, Env env, Obj args) {
            var enumerator = args.GetListEnumerator();
            enumerator.GetNext(out var oType);
            var type = vm.ExpectValue<Type>(oType);

            enumerator.GetNext(out var oMethod);
            var method = vm.Expect<SymObj>(oMethod).name;

            object self = null;
            if (enumerator.GetNext(out var oSelf)) {
                if (oSelf == Obj.nil) self = null;
                else self = vm.Expect<ValueObj>(oSelf).untypedValue;
            }

            return Call(vm, env, type, self, method, enumerator, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        }

        // clr-call-member self 'method arg1 arg2 ...
        private static Obj Prim_CallMember(VM vm, Env env, Obj args) {
            var enumerator = args.GetListEnumerator();

            enumerator.GetNext(out var oSelf);
            object self = vm.Expect<ValueObj>(oSelf).untypedValue;

            enumerator.GetNext(out var oMethod);
            var method = vm.Expect<SymObj>(oMethod).name;

            return Call(vm, env, self.GetType(), self, method, enumerator, BindingFlags.Public | BindingFlags.Instance);
        }

        private static Obj GetMethods(VM vm, Obj args, BindingFlags bindingFlags) {
            var typeObj = vm.ExpectList1(args);
            var type = vm.ExpectValue<Type>(typeObj);
            Obj head = Obj.nil;
            foreach (var m in type.GetMethods(bindingFlags)) {
                head = VM.Cons(VM.Intern(m.Name), head);
            }

            foreach (var p in type.GetProperties(bindingFlags)) {
                head = VM.Cons(VM.Intern("@" + p.Name), head);
            }

            foreach (var f in type.GetFields(bindingFlags)) {
                head = VM.Cons(VM.Intern("@:" + f.Name), head);
            }

            return VM.Reverse(head);
        }

        private static Obj Prim_Methods(VM vm, Env env, Obj args) {
            return GetMethods(vm, args, BindingFlags.Public | BindingFlags.Instance);
        }

        private static Obj Prim_StaticMethods(VM vm, Env env, Obj args) {
            return GetMethods(vm, args, BindingFlags.Public | BindingFlags.Static);
        }

        private static IEnumerable<string> GetNamespacesInContext(Env env) {
            for (var p = env; p != null; p = p.up) {
                var nsObj = env.FindLocal(namespacesSym) as ValueObj<HashSet<string>>;
                if (nsObj != null) {
                    foreach (var ns in nsObj.value)
                        yield return ns;
                }
            }
        }

        // (clr-using namespace) import namespace in local scope
        private static Obj Prim_Using(VM vm, Env env, Obj args) {
            var namespaceName = vm.ExpectList1(args);

            HashSet<string> ns;
            var nsObj = env.FindLocal(namespacesSym);
            if (nsObj == null) {
                ns = new HashSet<string>();
                env.AddVariable(namespacesSym, new ValueObj<HashSet<string>>(ns));
            }
            else {
                ns = (nsObj as ValueObj<HashSet<string>>).value;
            }
            
            ns.Add(vm.ExpectValue<string>(namespaceName));
            return Obj.nil;
        }

        private static Type GetType(string typeName, Env env) {
            foreach (var ass in assemblies) {
                var t = ass.GetType(typeName);
                if (t != null)
                    return t;
            }
            foreach (var ns in GetNamespacesInContext(env)) {
                var fullName = $"{ns}.{typeName}";
                foreach (var ass in assemblies) {
                    var t = ass.GetType(fullName);
                    if (t != null)
                        return t;
                }
            }

            return null;
        }

        // (clr-type name) or (clr-type 'name)
        private static Obj Prim_Type(VM vm, Env env, Obj args) {
            var typeNameObj = vm.ExpectList1(args);
            var type = GetType(
                vm.ExpectOr(typeNameObj,
                    Expect.On<SymObj, string>(s => s.name),
                    Expect.OnValue<string, string>(o => o)),
                env);
            return type != null ? (Obj)new ValueObj<Type>(type): Obj.nil;
        }

        private static Obj Prim_Types(VM vm, Env env, Obj args) {
            Obj head = Obj.nil;
            foreach (var ns in GetNamespacesInContext(env)) {
                var regex = new Regex(ns + @"\.[\w\d]+$");
                foreach (var ass in assemblies) {
                    foreach (var type in ass.GetTypes()) {
                        if (type.FullName != null && regex.IsMatch(type.FullName))
                            head = VM.Cons(new ValueObj<Type>(type), head);
                    }
                }
            }

            return VM.Reverse(head);
        }
    }
}