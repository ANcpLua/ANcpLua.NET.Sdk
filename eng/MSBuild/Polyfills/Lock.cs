// Copyright (c) ANcpLua. All rights reserved.
// Licensed under the MIT License.

#if !NET9_0_OR_GREATER

namespace System.Threading;

/// <summary>
/// Provides a way to use the System.Threading.Lock class in older frameworks.
/// </summary>
internal sealed class Lock
{
    private readonly object _lock = new();

    /// <summary>
    /// Enters the lock.
    /// </summary>
    public void Enter()
    {
        Monitor.Enter(_lock);
    }

    /// <summary>
    /// Exits the lock.
    /// </summary>
    public void Exit()
    {
        Monitor.Exit(_lock);
    }

    /// <summary>
    /// Enters the lock and returns a scope that exits the lock when disposed.
    /// </summary>
    public Scope EnterScope()
    {
        Monitor.Enter(_lock);
        return new Scope(_lock);
    }

    /// <summary>
    /// A ref struct that exits the lock when disposed.
    /// </summary>
    public readonly ref struct Scope
    {
        private readonly object _lock;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scope"/> struct.
        /// </summary>
        public Scope(object l)
        {
            _lock = l;
        }

        /// <summary>
        /// Exits the lock.
        /// </summary>
        public void Dispose()
        {
            Monitor.Exit(_lock);
        }
    }
}

#endif
