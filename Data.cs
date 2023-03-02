// https://github.com/kofifus/F/wiki

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace F {
  using F.Collections;
  using F.State;

  [FIgnore]
  public static class Data {

    // FIgnore excludes the class/field/property from checking 
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public sealed class FIgnore : Attribute { }

    [FIgnore]
    static ImmutableDictionary<Type,bool> VerifiedLogic = ImmutableDictionary<Type, bool>.Empty;
    static ImmutableDictionary<Type,bool> VerifiedData = ImmutableDictionary<Type, bool>.Empty;

    public static void AssertF(Func<Type, bool>? customIgnoreFunc=null, Func<Type, bool>? ApprovedDataFunc=null) {
#if DEBUG

      bool IgnoreFunc(Type t) {
        string fn = t.FullName ?? t.Name, ns = t.Namespace ?? "";
        if (ns == "F" || ns.StartsWith("F.State")) return true;
        if (fn.StartsWith("Microsoft.CodeAnalysis")
          || fn.StartsWith("System.Runtime.CompilerServices")
          || fn.StartsWith("<>f__AnonymousType")) return true;
        return customIgnoreFunc is not null && customIgnoreFunc(t);
      }

      foreach (var t in Assembly.GetExecutingAssembly().GetTypes()) {
        if (IgnoreFunc(t)) continue;
        if (t.GetCustomAttribute<FIgnore>() != null) continue;

        var isLogic = IsLogic($"_{t.Name}", t, ImmutableList<Type>.Empty, null, ApprovedDataFunc);
        if (isLogic == "") continue;
        var isData = IsData($"_{t.Name}", t, ImmutableList<Type>.Empty, ApprovedDataFunc);
        if (isData == "") continue;

        // we now call again to retrieve the correct message
        isLogic = IsLogic(t.Name, t, ImmutableList<Type>.Empty, null, ApprovedDataFunc);
        isData = IsData(t.Name, t, ImmutableList<Type>.Empty, ApprovedDataFunc);
        throw new($"Invalid type {ExpandTypeName(t)}:\nNot Data: {isData}\nNot Logic: {isLogic}");
      }

      VerifiedLogic = ImmutableDictionary<Type, bool>.Empty;
      VerifiedData = ImmutableDictionary<Type, bool>.Empty;
#endif
    }

    // wrapper around IsLogicInternal that tries a cache hit if prefix starts with "_"
    static string IsLogic(string prefix, Type t, ImmutableList<Type> parents, ImmutableHashSet<(Type, MemberInfo)>? members, Func<Type, bool>? ApprovedDataFunc) {
      if (!prefix.StartsWith("_")) return IsLogicInternal(prefix, t, parents, members, ApprovedDataFunc);

      if (VerifiedLogic.TryGetValue(t, out var b)) return b ? "" : $"{t} not Logic";
      var isLogic = IsLogicInternal(prefix, t, parents, members, ApprovedDataFunc);
      VerifiedLogic = VerifiedLogic.SetItem(t, isLogic=="");
      Debug.WriteLine($"|| IsLogic {t.FullName} {(isLogic == "" ? "True" : "False")}");
      return isLogic;
    }

    static string IsLogicInternal(string prefix, Type t, ImmutableList<Type> parents, ImmutableHashSet<(Type, MemberInfo)>? members, Func<Type, bool>? ApprovedDataFunc) {
      parents = parents.Add(t);

      members ??= GetFieldsAndProperties(t)
        .Select(vt => (memberType: vt.Item1, memberInfo: vt.Item2))
        .Where(vt => !vt.memberType.Equals(t))
        .Where(vt => !parents.Contains(vt.memberType)) // avoid recursion
        .ToImmutableHashSet();

      foreach (var (memberType, memberInfo) in members) {
        if (memberInfo.GetCustomAttribute<FIgnore>() != null) continue;

        if (memberType.IsPrimitive) return $"{prefix}.{memberInfo.Name} cannot be a basic type";

        if (t.IsClass) {
          var isRecord = t.GetMethod("<Clone>$") is not null;
          if (isRecord) return $"{prefix} cannot be a record";
        }

        var isState = ImplementsOrDerives(memberType, typeof(IReadOnlyState<>)) || ImplementsOrDerives(memberType, typeof(IState<>));
        if (isState) {
          var isPublic = IsMemeberPublic(memberInfo);
          if (isPublic) return $"{prefix}.{memberInfo.Name} cannot be a pubic State";
          continue;
        }

        var isLogic = IsLogic($"{prefix}.{memberInfo.Name}" , memberType, parents.Add(memberType), null, ApprovedDataFunc);

        if (isLogic != "") return isLogic;
      }

      var parameters = GetMethodParameters(t, parents);
      foreach (var (methodInfo, ps) in parameters) {
        if (methodInfo.GetCustomAttribute<FIgnore>() != null) continue;

        foreach (var p in ps) {
          var isState = ImplementsOrDerives(p.ParameterType, typeof(IReadOnlyState<>)) || ImplementsOrDerives(p.ParameterType, typeof(IState<>));
          if (isState) continue;

          if (p.ParameterType.GetCustomAttribute<FIgnore>() != null) continue;

          var isData = IsData($"{prefix}.{methodInfo.Name}_{p.Name}", p.ParameterType, parents.Add(t), ApprovedDataFunc);
          var isLogic = IsLogic($"{prefix}.{methodInfo.Name}_{p.Name}", p.ParameterType, parents.Add(t), null, ApprovedDataFunc);
          if (isData != "" && isLogic != "") return isData;
        }
      }

      return "";
    }

    // wrapper around IsDataInternal that tries a cache hit if prefix starts with "_"
    static string IsData(string prefix, Type t, ImmutableList<Type> parents, Func<Type, bool>? ApprovedDataFunc) {
      if (!prefix.StartsWith("_")) return IsDataInternal(prefix, t, parents, ApprovedDataFunc);

      if (VerifiedData.TryGetValue(t, out var b)) return b ? "" : $"{t} not Data";
      var isData = IsDataInternal(prefix, t, parents, ApprovedDataFunc);
      VerifiedData = VerifiedData.SetItem(t, isData=="");
      Debug.WriteLine($"|| IsData {t.FullName} {(isData == "" ? "True" : "False")}");
      return isData;
    }

    static string IsDataInternal(string prefix, Type t, ImmutableList<Type> parents, Func<Type, bool>? ApprovedDataFunc) {
      if (IsWhitelistedData(t)) return "";
      if (ApprovedDataFunc is object && ApprovedDataFunc(t)) return "";
      if (t.GetCustomAttribute<FIgnore>() != null) return "";

      if (t.IsEnum) return "";

      string fullName = t.FullName ?? "", baseFullName = t.BaseType?.FullName ?? "", ns = t.Namespace ?? "", name = t.Name ?? "", baseName = t.BaseType?.Name ?? "";

      if (IsCompilerGenerated(t)) return "";
      if (ns== "F.Collections") return "";

      var isAnonymous = name.Contains("AnonymousType");
      if (isAnonymous) return "";

      var isAttribute = t.BaseType == typeof(Attribute);
      if (isAttribute) return "";

      if (ns == "F.State") return $"{prefix} cannot be a State";

      if (t.IsClass) {
        var isRecord = t.GetMethod("<Clone>$") is not null;
        if (!isRecord) return $"{prefix} cannot be a class";
      }

      // check that all generic arguments are Data
      var genericArgs = t.GetGenericArguments().ToList();
      if (t.BaseType is object) genericArgs.AddRange(t.BaseType.GetGenericArguments());
      foreach (var gat in genericArgs) {
        if (gat == t) continue;
        if (gat.FullName is null) continue;
        if (gat.GetCustomAttributes(false).Any(a => a.GetType() == typeof(FIgnore))) continue;
        var isData = IsData($"{prefix}<{gat.FullName}>", gat, parents.Add(t), ApprovedDataFunc);
        if (isData != "") return isData;
      }

      var fieldsAndProperties = GetFieldsAndProperties(t)
        .Select(vt => (memberType: vt.Item1, memberInfo: vt.Item2))
        .Where(vt => vt.memberType!=t && !parents.Contains(vt.memberInfo)) // avoid recursion
        .Where(vt => !IsConst(vt.memberInfo)) // ignore consts
        .ToImmutableHashSet();

      var methodParameters = GetMethodParameters(t, parents);

      // allow classes with no members (ie attributes)
      if (fieldsAndProperties.IsEmpty && methodParameters.IsEmpty) return "";

      //var isNullable = Nullable.GetUnderlyingType(t) != null;

      foreach (var (memberType, memberInfo) in fieldsAndProperties) {
        if (memberInfo.GetCustomAttribute<FIgnore>() != null) continue;
        if ((memberInfo.DeclaringType?.Namespace ?? "") == "F.Collections") continue;

        var isData = IsData($"{prefix}.{memberInfo.Name}", memberType, parents.Add(t), ApprovedDataFunc);
        if (isData != "") return isData;
      }

      foreach (var (methodInfo, ps) in methodParameters) {
        if (methodInfo.GetCustomAttribute<FIgnore>() != null) continue;
        if ((methodInfo.DeclaringType?.Namespace ?? "") == "F.Collections") continue;

        foreach (var p in ps) {
          var pt = p.ParameterType.GetElementType() ?? p.ParameterType;
          if (pt.GetCustomAttribute<FIgnore>() != null) continue;
          var isData = IsData($"{prefix}.{methodInfo.Name}_{p.Name}", pt, parents.Add(t), ApprovedDataFunc);
          if (isData != "") return isData;
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

    static bool IsWhitelistedData(Type t_) {
      var basic = new Type[] {
      typeof(byte), typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(ushort), typeof(uint), typeof(ulong),
      typeof(char), typeof(float), typeof(double), typeof(decimal), typeof(bool), typeof(string), typeof(DBNull), typeof(Uri), 
      typeof(void), typeof(Guid), typeof(DateTime),
      typeof(byte?), typeof(sbyte?), typeof(short?), typeof(int?), typeof(long?), typeof(ushort?), typeof(uint?), typeof(ulong?),
      typeof(char?), typeof(float?), typeof(double?), typeof(decimal?), typeof(bool?), typeof(DateTime?),
      typeof(HttpContent)
      };

      var t = Nullable.GetUnderlyingType(t_) ?? t_;
      if (basic.Contains(t)) return true;

      if (typeof(ITuple).IsAssignableFrom(t)) return true; //Tuple
      if (typeof(Delegate).IsAssignableFrom(t)) return true; // Delegate

      return false;
    }

    static ImmutableHashSet<(Type, MemberInfo)> GetFieldsAndProperties(Type? type) {
      if (type is null) return ImmutableHashSet<(Type, MemberInfo)>.Empty; ;

      var bindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic;

      var res = type.GetMembers(bindingFlags)
        .Where(mi => IsFieldOrProperty(mi) && !IsIgnored(mi) && !IsCompilerGenerated(mi))
        .Where(mi => !mi.Name.Contains("EqualityContract"))
        .Select(mi => (GetUnderlyingType(mi)!, mi))
        .ToImmutableHashSet();

      // get base class members if any
      if (!Equals(type.BaseType, typeof(object))) res = res.Union(GetFieldsAndProperties(type.BaseType));

      return res;
    }

    static ImmutableDictionary<MethodBase, ImmutableHashSet<ParameterInfo>> GetMethodParameters(Type? type, ImmutableList<Type> parents) {
      var res = ImmutableDictionary<MethodBase, ImmutableHashSet<ParameterInfo>>.Empty;
      if (type is null) return res;

      var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(m => !m.IsSpecialName)
        .Where(m => !m.Name.StartsWith('<'))
        .Select(m => (MethodBase)m)
        .ToImmutableList()
        .AddRange(type.GetConstructors()) // can be unremarked when record ctors have CompilerGenerated
        .Where(m => m.GetCustomAttribute<CompilerGeneratedAttribute>() == null);

      foreach (var methodInfo in methods) {
        var ps = methodInfo.GetParameters()
          .ToImmutableList()
          // .Add(methodInfo.ReturnParameter)
          .Where(p => p.GetCustomAttribute<FIgnore>() == null)
          .Where(p => !parents.Contains(p.ParameterType)) // ignore cyclic dependencies
          .Where(p => !p.ParameterType.Equals(type)); // ignore self reference

        res = res.Add(methodInfo, ps.ToImmutableHashSet());
      }

      return res;
    }

    static bool IsFieldOrProperty(MemberInfo mi) => GetUnderlyingType(mi) != null;

    //static bool IsBackingField(MemberInfo mi) => mi is FieldInfo && mi.GetCustomAttribute<CompilerGeneratedAttribute>() == null;

    static bool IsIgnored(MemberInfo mi) => mi.GetCustomAttribute<FIgnore>() != null;

    static bool IsCompilerGenerated(MemberInfo mi) =>
      mi.GetCustomAttribute<CompilerGeneratedAttribute>() != null
      || (IsFieldOrProperty(mi) && (GetUnderlyingType(mi)!.Name.Contains("__") || GetUnderlyingType(mi)!.Name.Contains("<>c")));

    static bool IsCompilerGenerated(Type t) =>
      t.GetCustomAttribute<CompilerGeneratedAttribute>() != null|| t.Name.Contains("__") || t.Name.Contains("<>c");

    static bool ImplementsOrDerives(this Type t, Type from) {
      if (from is null) return false;
      if (!from.IsGenericType)  return from.IsAssignableFrom(t);
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

    /*
    static bool IsParams(ParameterInfo param) {
      return param.IsDefined(typeof(ParamArrayAttribute), false);
    }

    static bool IsPropertyWithBackingField(MemberInfo mi) => mi is PropertyInfo pi && GetBackingField(pi) is not null;

    static FieldInfo? GetBackingField(PropertyInfo pi) {
      if (pi is null) return null;
      if (!pi.CanRead) return null;
      var getMethod = pi.GetGetMethod(nonPublic: true);
      if (getMethod is object && !getMethod.IsDefined(typeof(CompilerGeneratedAttribute), inherit: true)) return null;
      var backingField = pi.DeclaringType?.GetField($"<{pi.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
      if (backingField == null) return null;
      if (!backingField.IsDefined(typeof(CompilerGeneratedAttribute), inherit: true)) return null;
      return backingField;
    } */

    /* public static bool IsMemberStatic(MemberInfo memberInfo) {
      return memberInfo.MemberType switch {
        MemberTypes.Field => ((FieldInfo)memberInfo).IsStatic,
        MemberTypes.Property => ((PropertyInfo)memberInfo).GetAccessors(true)[0].IsStatic,
        _ => false
      };
    } */

    /* // check that all generic arguments are Data ???
    var genericArgs = t.GetGenericArguments().ToList();
    if (t.Bas eType is object) genericArgs.AddRange(t.BaseType.GetGenericArguments());
    foreach (var gat in genericArgs) {
      if (gat == t) continue;
      if (gat.FullName is null) continue;
      if (gat.GetCustomAttributes(false).Any(a => a.GetType() == typeof(FIgnore))) continue;
      if (!Fcheck(gat, false, $"{t.Name} generic argument ", assert, ApproveFunc)) return false;
    }

    static bool IsReadonlyAfterInit(MemberInfo memberInfo) {
      switch (memberInfo.MemberType) {
        case MemberTypes.Field:
          var fieldInfo = ((FieldInfo)memberInfo);
          return fieldInfo.IsInitOnly || fieldInfo.IsLiteral;
        case MemberTypes.Property:
          var propertyInfo = (PropertyInfo)memberInfo;
          if (!propertyInfo.CanWrite) return true;
          var setMethodReturnParameterModifiers = propertyInfo.SetMethod?.ReturnParameter.GetRequiredCustomModifiers();
          return setMethodReturnParameterModifiers?.Contains(typeof(IsExternalInit)) ?? false;
      };
      return false;
    }


      */
  }
}



