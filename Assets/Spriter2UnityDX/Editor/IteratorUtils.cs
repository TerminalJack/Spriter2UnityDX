using System;
using System.Collections;

namespace Spriter2UnityDX
{
    public static class IteratorUtils
    {
        public static IEnumerator SafeEnumerable(
            Func<IEnumerator> enumeratorFactory,
            Action<Exception> onError = null,
            Action onCompleted = null)
        {
            IEnumerator iterator;

            try
            {
                iterator = enumeratorFactory();
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
                yield break;
            }

            while (true)
            {
                bool hasNext;

                try
                {
                    hasNext = iterator.MoveNext();
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex);
                    yield break;
                }

                if (!hasNext)
                {
                    break;
                }

                yield return iterator.Current;
            }

            onCompleted?.Invoke();
        }
    }
}