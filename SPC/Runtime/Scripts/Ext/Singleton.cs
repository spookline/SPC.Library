using System;

namespace Spookline.SPC.Ext {
    /// <summary>
    ///     Provides a generic implementation of the Singleton design pattern.
    ///     Ensures that only one instance of the given type <typeparamref name="T" /> can exist.
    /// </summary>
    /// <typeparam name="T">The type of the class that will implement the Singleton pattern.</typeparam>
    public abstract class Singleton<T> {

        private static T _instance;

        public static T Instance => _instance ??= Activator.CreateInstance<T>();

        public static bool IsInitialized => _instance != null;

    }
}