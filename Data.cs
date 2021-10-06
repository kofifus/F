using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

// https://github.com/kofifus/F/wiki

#nullable enable

namespace F {

  public static class Data {

    // FIgnore excludes the class/field/property from checking 
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class FIgnore : Attribute { }

    [FIgnore]
    static ImmutableDictionary<Type, bool> Verified = ImmutableDictionary<Type, bool>.Empty;

    public static void AssertF(Func<Type, bool>? IgnoreFunc=null, Func<Type, bool>? ApproveFunc=null) {
#if DEBUG
      foreach (var t in Assembly.GetExecutingAssembly().GetTypes()) {
        if (IgnoreFunc is object && IgnoreFunc(t)) continue;
        Fcheck(t, true, "", true, ApproveFunc);
      }
#endif
    }

    static bool Fcheck(Type t, bool checkFIgnore, string prefix, bool assert, Func<Type, bool>? ApproveFunc) {
      if (!Verified.TryGetValue(t, out var isData)) {
        isData = FcheckInternal(t, checkFIgnore, prefix, assert, ApproveFunc);
        Verified = Verified.Add(t, isData); // cache it
      }
      return isData;
    }

    static bool FcheckInternal(Type t, bool checkFIgnore, string prefix, bool assert, Func<Type, bool>? ApproveFunc) {
      if (IsWhitelisted(t)) return true;
      if (ApproveFunc is object && ApproveFunc(t)) return true;

      string fullName = (t.FullName ?? ""), baseFullName = (t.BaseType?.FullName ?? ""), ns = (t.Namespace ?? ""), name = (t.Name ?? ""), baseName = (t.BaseType?.Name ?? "");
      var isAnonymous = name.Contains("AnonymousType");
      var isAttribute = t.BaseType == typeof(Attribute);
      var isCompilerGenerated = Attribute.GetCustomAttribute(t, typeof(CompilerGeneratedAttribute)) is object;
      if (fullName.Contains("__")) isCompilerGenerated = true;
      if (t.IsInterface || isAnonymous || isAttribute || isCompilerGenerated) return true;

      if (checkFIgnore && t.GetCustomAttributes(false).Any(a => a.GetType() == typeof(FIgnore))) return true;

      Debug.WriteLine($"Fcheck({t.Name}, {checkFIgnore}, {prefix}, {assert})");

      // check that all generic arguments are Data ???
      //foreach (var gat in t.GetGenericArguments()) {
      //  if (gat.GetCustomAttributes(false).Any(a => a.GetType() == typeof(FIgnore))) continue;
      //  if (!Fcheck(gat, false, $"({t.Name} generic argument) ", assert)) return false;
      //}

      // check for State
      if (ns == "F" && (t.ImplementsOrDerives(typeof(IStateVal<>)) || t.ImplementsOrDerives(typeof(IStateRef<>)) || t.ImplementsOrDerives(typeof(State<>.Combine<>)))) return true;

      var members = GetMembers(t, true, true, checkFIgnore ? typeof(FIgnore) : null);

      // allow classes with no members (ie attributes)
      if (members.IsEmpty) return true;

      //if (t.IsGenericType || t.IsGenericTypeParameter) return true;

      var isStruct = t.IsValueType && !t.IsEnum;
      var isStaticClass = t.IsClass && t.IsSealed && t.IsAbstract;

      if (!isStruct && !isStaticClass && !t.IsEnum)
      {
        // type must implement IEquatable<T> - value semantics
        if (!t.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEquatable<>)))
        {
          if (assert) throw new($"{prefix}{t.FullName ?? t.Name} not inherting IEquatable<T>");
          return false;
        }

        // type must implement == and !=
        var operators = t.GetMethods().Where(methodInfo => methodInfo.IsSpecialName).Select(methodInfo => methodInfo.Name);
        if (!operators.Contains("op_Equality"))
        {
          if (assert) throw new($"{prefix}{t.FullName ?? t.Name} does not implement operator==");
          return false;
        }
        if (!operators.Contains("op_Inequality"))
        {
          if (assert) throw new($"{prefix}{t.FullName ?? t.Name} does not implement operator!=");
          return false;
        }
      }

      foreach (var (memberType, memberInfo) in members) {
        // process [FIgnore] fields 
        if (memberInfo.GetCustomAttributes(false).Any(a => a.GetType() == typeof(FIgnore))) {
          var isPublic = memberInfo.MemberType switch {
            MemberTypes.Field => ((FieldInfo)memberInfo).IsPublic,
            MemberTypes.Property => ((PropertyInfo)memberInfo).GetAccessors().Any(MethodInfo => MethodInfo.IsPublic),
            _ => false
          };

          /*if (isPublic) {
            if (assert) throw new($"[FIgnore] on public ({t.Name} member) {memberInfo.Name}");
            return false;
          }*/

          continue;
        }

        // ignore the type that is currently checked to avoid recursion
        if (memberType == t) continue;

        if (memberInfo.Name == "EqualityContract") continue;

        // check member is read only
        if (IsWhitelisted(memberType) && !IsReadonlyAfterInit(memberInfo)) {
          if (assert) throw new($"({t.Name} member) {memberInfo.Name} is not read only");
          return false;
        }

        if (memberType.IsEnum) continue;

        if (!Fcheck(memberType, false, $"({t.Name} member) ", assert, ApproveFunc)) return false;
      }
      return true;
    }

    static bool IsWhitelisted(Type t_) {
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

    static ImmutableList<(Type, MemberInfo)> GetMembers(Type? type, bool getStatic = true, bool getPrivate = true, Type? ignoreAttribute = null) {
      var res = ImmutableList<(Type, MemberInfo)>.Empty;
      if (type is null) return res;

      // get base class members if any
      if (!object.Equals(type.BaseType, typeof(Object))) res = res.AddRange(GetMembers(type.BaseType, getStatic, getPrivate, ignoreAttribute));

      var bindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public;
      if (getStatic) bindingFlags |= BindingFlags.Static;
      if (getPrivate) bindingFlags |= BindingFlags.NonPublic;

      IEnumerable<MemberInfo> membersInfo = type.GetMembers(bindingFlags);
      if (ignoreAttribute is object) membersInfo = membersInfo.Where(memberInfo => !memberInfo.GetCustomAttributes(false).Any(a => a.GetType() == ignoreAttribute));
      // filter out fields which are backing for properties
      membersInfo = membersInfo.Where(memberInfo => memberInfo is PropertyInfo || (memberInfo is FieldInfo && memberInfo.GetCustomAttribute<CompilerGeneratedAttribute>() == null));

      // filter out properties with no backing fields
      membersInfo = membersInfo.Where(memberInfo => memberInfo is FieldInfo || (memberInfo is PropertyInfo propertyInfo && GetBackingField(propertyInfo).Item1 == true));

      foreach (var memberInfo in membersInfo) {
        var memberType = memberInfo.MemberType switch {
          MemberTypes.Field => ((FieldInfo)memberInfo).FieldType,
          MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType,
          //MemberTypes.Event => ((EventInfo)memberInfo).EventHandlerType,
          //MemberTypes.Method => ((MethodInfo)memberInfo).ReturnType,
          _ => null
        };
        if (memberType is null) continue;

        res = res.Add((memberType, memberInfo));
      }

      return res;
    }

    static (bool, FieldInfo) GetBackingField(PropertyInfo pi) {
      if (pi is null) return default;
      if (!pi.CanRead) return default;
      var getMethod = pi.GetGetMethod(nonPublic: true);
      if (getMethod is object && !getMethod.IsDefined(typeof(CompilerGeneratedAttribute), inherit: true)) return default;
      var backingField = pi.DeclaringType?.GetField($"<{pi.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
      if (backingField == null) return default;
      if (!backingField.IsDefined(typeof(CompilerGeneratedAttribute), inherit: true)) return default;
      return (true, backingField);
    }

    public static bool ImplementsOrDerives(this Type @this, Type from) {
      if (from is null) return false;
      if (!from.IsGenericType)  return from.IsAssignableFrom(@this);
      if (!from.IsGenericTypeDefinition) return from.IsAssignableFrom(@this);
      
      if (from.IsInterface) {
        foreach (Type @interface in @this.GetInterfaces()) {
          if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == from) {
            return true;
          }
        }
      }

      if (@this.IsGenericType && @this.GetGenericTypeDefinition() == from) return true;
      return @this.BaseType?.ImplementsOrDerives(from) ?? false;
    }
  }
}


