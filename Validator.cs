// https://github.com/kofifus/F/wiki

#nullable enable

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace F;

using F.State;
using System;
using System.Linq;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
public sealed class FIgnore : Attribute { }

[FIgnore]
public static class Validator {
  static ImmutableDictionary<Type, bool> VerifiedLogic = ImmutableDictionary<Type, bool>.Empty;
  static ImmutableDictionary<Type, bool> VerifiedData = ImmutableDictionary<Type, bool>.Empty;
  static readonly Type[] AllTypes = Assembly.GetExecutingAssembly().GetTypes();
  static Func<Type, bool>? CustomIgnoreFunc = null;
  static Func<Type, bool>? ApprovedDataFunc = null;

  static bool IgnoreFunc(Type t) {
    string n = t.Name, fn = t.FullName ?? n, ns = t.Namespace ?? "";
    if (ns == "F" || ns.StartsWith("F.State")) return true;
    if (fn.StartsWith("Microsoft.CodeAnalysis")) return true;
    if (fn.StartsWith("System.Runtime.CompilerServices")) return true;
    if (fn.StartsWith("<>f__AnonymousType")) return true;

    if (t.GetCustomAttribute<FIgnore>() != null) return true;
    if (t.IsEnum) return true;
    if (n.Contains("AnonymousType")) return true;
    if (t.BaseType == typeof(Attribute)) return true;
    if (n.StartsWith('<')) return true;
    if (typeof(Delegate).IsAssignableFrom(t)) return true;

    if (CustomIgnoreFunc is not null && CustomIgnoreFunc(t)) return true;

    return false;
  }

  public static void Run(Func<Type, bool>? customIgnoreFunc = null, Func<Type, bool>? approvedDataFunc = null) {
#if DEBUG
    CustomIgnoreFunc = customIgnoreFunc;
    ApprovedDataFunc = approvedDataFunc;

    foreach (var t in AllTypes) FValidate(t);

    // cleanup
    VerifiedLogic.Clear();
    VerifiedData.Clear();
#endif
  }

  public static void FValidate(Type t) {
    var name = t.Name;

    if (IsRecord(t)) {
      if (IsData($"_{name}", t, []) == "") return;
      var msg = IsData(name, t, []);
      throw new($"Data record {msg}");
    } else {
      if (IsLogic($"_{name}", t, []) == "") return;
      var msg = IsLogic(name, t, []);
      throw new($"Logic class {msg}");
    }
  }

  // wrapper around IsDataInternal that tries a cache hit if prefix starts with "_"
  static string IsData(string prefix, Type t, ImmutableList<Type> parents) {
    if (!prefix.StartsWith('_')) return IsDataInternal(prefix, t, parents); // don't use cache
    if (VerifiedData.TryGetValue(t, out var verified)) return verified ? "" : $"{prefix}{t} not Data"; // try a cache hit

    if (parents.Count > 0 && AllTypes.Contains(t)) FValidate(t); // new type
    var isData = IsDataInternal(prefix, t, parents);
    VerifiedData = VerifiedData.SetItem(t, isData == "");
    //Debug.WriteLine($"|| IsData {t.FullName} {(isData == "" ? "True" : "False")}");
    return isData;
  }

  static string IsDataInternal(string prefix, Type t, ImmutableList<Type> parents) {

    string fullName = t.FullName ?? "", baseFullName = t.BaseType?.FullName ?? "", ns = t.Namespace ?? "", name = t.Name ?? "", baseName = t.BaseType?.Name ?? "";
    if (IgnoreFunc(t)) return "";
    if (IsApprovedData(t)) return "";

    if (ns == "F.State") return $"{prefix} cannot be a State";

    if (!IsRecord(t)) return $"{prefix} cannot be a class";

    // check that all generic arguments are Data
    var genericArgumentsIsData = AreGenericArgumentsData(prefix, t, t, parents);
    if (genericArgumentsIsData != "") return genericArgumentsIsData;

    if (ns == "F.Collections") return "";

    var fieldsAndProperties = GetFieldsAndProperties(t)
      .Select(vt => (memberType: vt.Item1, memberInfo: vt.Item2))
      .Where(vt => vt.memberType != t && !parents.Contains(vt.memberInfo)) // avoid recursion
      .Where(vt => !IsConst(vt.memberInfo)) // ignore consts
      .ToImmutableHashSet();

    var methodsInfoDict = GetMethodsInfoDict(t, parents);

    // allow classes with no members (ie attributes)
    if (fieldsAndProperties.IsEmpty && methodsInfoDict.IsEmpty) return "";

    foreach (var (memberType, memberInfo) in fieldsAndProperties) {
      if ((memberInfo.DeclaringType?.Namespace ?? "") == "F.Collections") continue;

      var pPrefix = $"{prefix} member {memberInfo.Name}";
      var isData = IsData($"{pPrefix}", memberType, parents.Add(t));
      if (isData != "") return isData;
    }

    foreach (var (methodInfo, ps) in methodsInfoDict) {
      var mname = methodInfo.Name;
      if (methodInfo.GetCustomAttribute<FIgnore>() is object) continue;

      if ((methodInfo.DeclaringType?.Namespace ?? "") == "F.Collections") continue;
      if (mname == "GetHashCode") return $"{prefix} cannot have GetHashCode()";
      if (mname == "Equals") {
        var pars = methodInfo.GetParameters();
        if (pars.Length == 1 && pars.First().ParameterType == t) return $"{prefix} cannot have Equals(T)";
      }
        
      foreach (var p in ps) {
        var pPrefix = $"{prefix} method {mname} parameter {p.Name}";

        var isData = IsData($"{pPrefix}", p.ParameterType.GetElementType() ?? p.ParameterType, parents.Add(t));
        if (isData != "") return isData;
      }
    }

    return "";
  }

  // wrapper around IsLogicInternal that tries a cache hit if prefix starts with "_"
  static string IsLogic(string prefix, Type t, ImmutableList<Type> parents) {
    if (!prefix.StartsWith('_')) return IsLogicInternal(prefix, t, parents); // don't use cache
    if (VerifiedLogic.TryGetValue(t, out var verified)) return verified ? "" : $"{prefix}{t} not Logic"; // try a cache hit

    if (parents.Count > 0 && AllTypes.Contains(t)) FValidate(t); // new type
    var isLogic = IsLogicInternal(prefix, t, parents);
    VerifiedLogic = VerifiedLogic.SetItem(t, isLogic == "");
    //Debug.WriteLine($"|| IsLogic {t.FullName} {(isLogic == "" ? "True" : "False")}");
    return isLogic;
  }

  static string IsLogicInternal(string prefix, Type t, ImmutableList<Type> parents) {
    string fullName = t.FullName ?? "", baseFullName = t.BaseType?.FullName ?? "", ns = t.Namespace ?? "", name = t.Name ?? "", baseName = t.BaseType?.Name ?? "";
    if (IgnoreFunc(t)) return "";

    parents = parents.Add(t);

    if (IsRecord(t)) return $"{prefix} cannot be a record";

    ImmutableHashSet<(Type, MemberInfo)>? members = GetFieldsAndProperties(t)
      .Select(vt => (memberType: vt.Item1, memberInfo: vt.Item2))
      .Where(vt => !vt.memberType.Equals(t) && !parents.Contains(vt.memberType)) // avoid recursion
      .ToImmutableHashSet();

    foreach (var (memberType, memberInfo) in members) {
      var pPrefix = $"{prefix} member {memberInfo.Name}";

      if (memberType.IsPrimitive) return $"{pPrefix} cannot be a basic type";

      var isState = ImplementsOrDerives(memberType, typeof(IReadOnlyState<>)) || ImplementsOrDerives(memberType, typeof(IState<>));
      if (isState) {
        var isPublic = IsMemeberPublic(memberInfo);
        if (isPublic) return $"{pPrefix} cannot be a pubic State";

        var genericArgumentsIsData = AreGenericArgumentsData(prefix, t, memberType, parents);
        if (genericArgumentsIsData != "") return genericArgumentsIsData;

        continue;
      }

      var isLogic = IsLogic($"{pPrefix}", memberType, parents.Add(memberType));

      if (isLogic != "") return isLogic;
    }

    var methodsInfoDict = GetMethodsInfoDict(t, parents);
    foreach (var (methodInfo, ps) in methodsInfoDict) {
      foreach (var p in ps) {
        var isState = ImplementsOrDerives(p.ParameterType, typeof(IReadOnlyState<>)) || ImplementsOrDerives(p.ParameterType, typeof(IState<>));
        if (isState) {
          var genericArgumentsIsData = AreGenericArgumentsData(prefix, t, p.ParameterType, parents);
          if (genericArgumentsIsData != "") return genericArgumentsIsData;
          continue;
        }

        var pPrefix = $"{prefix} method {methodInfo.Name} parameter {p.Name}";
        var isData = IsData(pPrefix, p.ParameterType, parents.Add(t));
        var isLogic = IsLogic(pPrefix, p.ParameterType, parents.Add(t));
        if (isData != "" && isLogic != "") return isData;
      }
    }

    return "";
  }

  static string ExpandTypeName(Type t) {
    return !t.IsGenericType || t.IsGenericTypeDefinition
      ? !t.IsGenericTypeDefinition ? t.Name : (t.Name.Contains('`') ? t.Name.Remove(t.Name.IndexOf('`')) : t.Name)
      : $"{ExpandTypeName(t.GetGenericTypeDefinition())}<{string.Join(',', t.GetGenericArguments().Select(ExpandTypeName))}>";
  }

  static bool IsConst(MemberInfo mi) => mi.MemberType == MemberTypes.Field && ((FieldInfo)mi).IsLiteral;

  static bool IsRecord(Type t) => t.IsClass && t.GetMethod("<Clone>$") is not null;

  static bool IsApprovedData(Type t_) {
    var basic = new Type[] {
    typeof(byte), typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(ushort), typeof(uint), typeof(ulong),
    typeof(char), typeof(float), typeof(double), typeof(decimal), typeof(bool), typeof(string), typeof(DBNull), typeof(Uri),
    typeof(void), typeof(Guid), typeof(DateTime),
    typeof(byte?), typeof(sbyte?), typeof(short?), typeof(int?), typeof(long?), typeof(ushort?), typeof(uint?), typeof(ulong?),
    typeof(char?), typeof(float?), typeof(double?), typeof(decimal?), typeof(bool?), typeof(DateTime?),
    typeof(System.Net.Http.HttpContent)
    };

    var t = Nullable.GetUnderlyingType(t_) ?? t_;
    if (basic.Contains(t)) return true;

    if (typeof(ITuple).IsAssignableFrom(t)) return true;

    if (ApprovedDataFunc is object && ApprovedDataFunc(t)) return true;

    return false;
  }

  static ImmutableHashSet<(Type, MemberInfo)> GetFieldsAndProperties(Type? type) {
    if (type is null) return [];

    var bindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic;

    var res = type.GetMembers(bindingFlags)
        .Where(mi => IsFieldOrProperty(mi) && mi.GetCustomAttribute<FIgnore>() == null)
        .Where(mi => !mi.Name.Contains("EqualityContract"))
        .Select(mi => (type: GetUnderlyingType(mi)!, memberInfo: mi));

    // get base class members if any
    if (!Equals(type.BaseType, typeof(object))) res = res.Union(GetFieldsAndProperties(type.BaseType));

    res = res.Where(vt => vt.memberInfo.GetCustomAttribute<FIgnore>() == null);
    return res.ToImmutableHashSet();
  }

  static ImmutableDictionary<MethodBase, ImmutableHashSet<ParameterInfo>> GetMethodsInfoDict(Type? type, ImmutableList<Type> parents) {
    var res = ImmutableDictionary<MethodBase, ImmutableHashSet<ParameterInfo>>.Empty;
    if (type is null) return res;
    var n = type.Name;
    var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
      .Where(m => !m.IsSpecialName)
      .Where(m => !m.Name.StartsWith('<'))
      .Select(m => (MethodBase)m) // so that we can add constructors
      .ToImmutableList()
      .AddRange(type.GetConstructors()) // can be unremarked when record ctors have CompilerGenerated
      .Where(m => m.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
      .Where(m => m.GetCustomAttribute<FIgnore>() == null);

    foreach (var methodInfo in methods) {
      var ps = methodInfo.GetParameters()
        // .Add(methodInfo.ReturnParameter)
        .Where(p => p.GetCustomAttribute<FIgnore>() == null)
        .Where(p => !parents.Contains(p.ParameterType)) // ignore cyclic dependencies
        .Where(p => !p.ParameterType.Equals(type)) // ignore self reference
        .Where(p => p.GetCustomAttribute<FIgnore>() == null);

      res = res.Add(methodInfo, ps.ToImmutableHashSet());
    }

    return res;
  }

  static bool IsFieldOrProperty(MemberInfo mi) => GetUnderlyingType(mi) != null;

  //static bool IsBackingField(MemberInfo mi) => mi is FieldInfo && mi.GetCustomAttribute<CompilerGeneratedAttribute>() == null;


  /*static bool IsCompilerGenerated(MemberInfo mi) =>
    mi.GetCustomAttribute<CompilerGeneratedAttribute>() != null
    || (IsFieldOrProperty(mi) && (GetUnderlyingType(mi)!.Name.Contains("__") || GetUnderlyingType(mi)!.Name.Contains("<>c")));

  static bool IsCompilerGenerated(Type t) =>
    t.GetCustomAttribute<CompilerGeneratedAttribute>() != null || t.Name.Contains("__") || t.Name.Contains("<>c");
  */

  static bool ImplementsOrDerives(this Type t, Type from) {
    if (from is null) return false;
    if (!from.IsGenericType) return from.IsAssignableFrom(t);
    if (!from.IsGenericTypeDefinition) return from.IsAssignableFrom(t);

    if (from.IsInterface) {
      foreach (Type tinterface in t.GetInterfaces()) {
        if (tinterface.IsGenericType && tinterface.GetGenericTypeDefinition() == from) {
          return true;
        }
      }
    }

    if (t.IsGenericType && t.GetGenericTypeDefinition() == from) return true;
    return t.BaseType?.ImplementsOrDerives(from) ?? false;
  }

  static bool IsMemeberPublic(MemberInfo memberInfo) {
    return memberInfo.MemberType switch {
      MemberTypes.Field => ((FieldInfo)memberInfo).IsPublic,
      MemberTypes.Property => ((PropertyInfo)memberInfo).GetAccessors().Any(MethodInfo => MethodInfo.IsPublic),
      _ => false
    };
  }

  static Type? GetUnderlyingType(MemberInfo member) {
    return member.MemberType switch {
      MemberTypes.Field => ((FieldInfo)member).FieldType,
      MemberTypes.Property => ((PropertyInfo)member).PropertyType,
      _ => null
    };
  }

  // check that all generic arguments are Data
  static string AreGenericArgumentsData(string prefix, Type t, Type memberType, ImmutableList<Type> parents) {
    var genericArgs = memberType.GetGenericArguments().ToList();
    if (memberType.BaseType is object) genericArgs.AddRange(memberType.BaseType.GetGenericArguments());
    foreach (var gat in genericArgs) {
      if (gat.FullName is null) continue;
      if (gat == t) continue;
      if (parents.Contains(gat)) continue; // avoid recursion
      if (gat.GetCustomAttribute<FIgnore>() != null) continue;
      var isData = IsData($"{prefix} generic parameter {gat.Name}", gat, parents.Add(t));
      if (isData != "") return isData;
    }
    return "";
  }
}




