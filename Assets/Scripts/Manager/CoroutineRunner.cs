using System.Collections;
using UnityEngine;

public class CoroutineRunner : Singleton<CoroutineRunner>
{
    public static Coroutine StartRoutine(IEnumerator routine)
    {
        if (routine == null || Instance == null) return null;

        return Instance.StartCoroutine(routine);
    }

    public static void StopRoutine(Coroutine routine)
    {
        if (routine == null || Instance == null) return;

        Instance.StopCoroutine(routine);
    }

    public static void StopRoutine(IEnumerator routine)
    {
        if (routine == null || Instance == null) return;

        Instance.StopCoroutine(routine);
    }

    public static void StopAllRoutines()
    {
        if (Instance == null) return;

        Instance.StopAllCoroutines();
    }
}