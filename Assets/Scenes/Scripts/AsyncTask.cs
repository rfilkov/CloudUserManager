using System;
using System.Threading;
using UnityEngine;

public enum TaskState
{
    Running, Failed, Succeed
}

public class AsyncTask<T>
{
    public TaskState State { get; internal set; }
    public bool LogErrors { get; set; }
    public T Result { get { return _result; } }

    private Func<T> _action;
    private T _result;

    public AsyncTask(Func<T> backgroundAction)
    {
        this._action = backgroundAction;
    }

    public void Start()
    {
        State = TaskState.Running;
        ThreadPool.QueueUserWorkItem(state => DoInBackground());
    }

    private void DoInBackground()
    {
        _result = default(T);

        try
        {
            if (_action != null)
                _result = _action();

            State = TaskState.Succeed;
        }
        catch (Exception ex)
        {
            State = TaskState.Failed;

            if (LogErrors)
                Debug.LogException(ex);
        }
    }
}