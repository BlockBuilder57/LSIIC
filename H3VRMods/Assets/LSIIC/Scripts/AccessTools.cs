using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace HarmonyLib
{
    /// <summary>A helper class for reflection related functions</summary>
    public static class AccessTools
    {
        /// <summary>Shortcut for <see cref="BindingFlags"/> to simplify the use of reflections and make it work for any access level</summary>
        public static BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                         BindingFlags.Static | BindingFlags.GetField | BindingFlags.SetField |
                                         BindingFlags.GetProperty | BindingFlags.SetProperty;

        /// <summary>Shortcut for <see cref="BindingFlags"/> to simplify the use of reflections and make it work for any access level but only within the current type</summary>
        public static BindingFlags allDeclared = all | BindingFlags.DeclaredOnly;

        /// <summary>Gets the reflection information for a property by searching the type and all its super types</summary>
        /// <param name="type">The type</param>
        /// <param name="name">The name of the property</param>
        /// <returns>A <see cref="PropertyInfo"/> if property found, otherwise null</returns>
        public static PropertyInfo Property(Type type, string name)
        {
            if (type == null) throw new ArgumentNullException(type.Name);
            if (name == null) throw new ArgumentNullException(name);

            var property = FindIncludingBaseTypes(type, t => t.GetProperty(name, all));
            return property;
        }

        /// <summary>Gets the reflection information for a field by searching the type and all its super types</summary>
        /// <param name="type">The type where the field is defined</param>
        /// <param name="name">The name of the field (case sensitive)</param>
        /// <returns>A <see cref="FieldInfo"/> if field found, otherwise null</returns>
        public static FieldInfo Field(Type type, string name)
        {
            if (type == null) throw new ArgumentNullException(type.Name);
            if (name == null) throw new ArgumentNullException(name);

            var field = FindIncludingBaseTypes(type, t => t.GetField(name, all));
            return field;
        }

        /// <summary>Gets the reflection information for a method by searching the type and all its super types</summary>
        /// <param name="type">The type where the method is declared</param>
        /// <param name="name">The name of the method (case sensitive)</param>
        /// <param name="parameters">Optional parameters to target a specific overload of the method</param>
        /// <param name="generics">Optional list of types that define the generic version of the method</param>
        /// <returns>A <see cref="MethodInfo"/> if method found, otherwise null</returns>
        public static MethodInfo Method(Type type, string name, Type[] parameters = null, Type[] generics = null)
        {
            if (type == null) throw new ArgumentNullException(type.Name);
            if (name == null) throw new ArgumentNullException(name);

            try
            {
                MethodInfo result;
                var modifiers = new ParameterModifier[] { };
                if (parameters == null)
                    try
                    {
                        result = FindIncludingBaseTypes(type, t => t.GetMethod(name, all));
                    }
                    catch (AmbiguousMatchException ex)
                    {
                        result = FindIncludingBaseTypes(type, t => t.GetMethod(name, all, null, new Type[0], modifiers));

                        if (result == null)
                            throw new AmbiguousMatchException("Ambiguous match for {type}:{name}", ex);
                    }
                else
                    result = FindIncludingBaseTypes(type, t => t.GetMethod(name, all, null, parameters, modifiers));

                if (result == null)
                {
                    return null;
                }

                if (generics != null) result = result.MakeGenericMethod(generics);
                return result;
            }
            catch (AmbiguousMatchException ex)
            {
                throw new AmbiguousMatchException("Ambiguous match for {type}::{name}{genericPart}(${paramsPart})", ex);
            }
        }

        /// <summary>Applies a function going up the type hierarchy and stops at the first non null result</summary>
        /// <typeparam name="T">Result type of func()</typeparam>
        /// <param name="type">The type to start with</param>
        /// <param name="func">The evaluation function returning T</param>
        /// <returns>Returns the first non null result or default(T) when reaching the top level type object</returns>
        public static T FindIncludingBaseTypes<T>(Type type, Func<Type, T> func) where T : class
        {
            while (true)
            {
                var result = func(type);

                if (result != null) return result;

                if (type == typeof(object)) return default(T);
                type = type.BaseType;
            }
        }
    }
}