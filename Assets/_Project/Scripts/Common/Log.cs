#nullable enable
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DemonFighter.Common
{
    /// <summary>
    /// Project-wide logging with a category prefix, so Console output can be filtered per system
    /// and there is one place to redirect or silence logging later. Unity-side code never calls Debug.Log directly.
    /// </summary>
    public static class Log
    {
        public static void Info(string category, string message, Object? context = null)
        {
            Debug.Log(Format(category, message), context);
        }

        public static void Warn(string category, string message, Object? context = null)
        {
            Debug.LogWarning(Format(category, message), context);
        }

        public static void Error(string category, string message, Object? context = null)
        {
            Debug.LogError(Format(category, message), context);
        }

        public static void Error(string category, string message, Exception exception, Object? context = null)
        {
            Debug.LogError(Format(category, message), context);
            Debug.LogException(exception, context);
        }

        private static string Format(string category, string message)
        {
            return "[" + category + "] " + message;
        }
    }
}
