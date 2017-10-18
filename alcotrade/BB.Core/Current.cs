using System;

namespace BB.Core
{
    public static class Current<T> where T : class
    {
        public static T Init(T instance)
        {
            if (Instance != null)
                throw new InvalidOperationException();

            Instance = instance;
            return Instance;
        }

        public static T Instance { get; private set; }
    }
}