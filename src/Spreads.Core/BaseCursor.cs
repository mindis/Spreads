﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Spreads
{
    internal sealed class BaseCursorAsync<TK, TV, TCursor> : ICursor<TK, TV>
        where TCursor : ICursor<TK, TV>
    {
        //private static readonly BoundedConcurrentBag<BaseCursorAsync<TK, TV, TCursor>> Pool = new BoundedConcurrentBag<BaseCursorAsync<TK, TV, TCursor>>(Environment.ProcessorCount * 16);

        private ISeries<TK, TV> _source;

        // NB this is often a struct, should not be made readonly!
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        internal TCursor _innerCursor;

        private TaskCompletionSource<Task<bool>> _cancelledTcs;
        private CancellationTokenRegistration _registration;
        private CancellationToken _token;

        // NB factory could be more specific than GetCursor method of the source, which returns an interface
        // At the same time, we need access to BaseSeries members and cannot use Source property of the cursor
        public BaseCursorAsync(ISeries<TK, TV> source, Func<TCursor> cursorFactory)
        {
            _source = source;
            _innerCursor = cursorFactory();
        }

        public BaseCursorAsync(Func<TCursor> cursorFactory)
        {
            _innerCursor = cursorFactory();
            _source = _innerCursor.Source;
        }

        public static BaseCursorAsync<TK, TV, TCursor> Create(ISeries<TK, TV> source, Func<TCursor> cursorFactory)
        {
            return new BaseCursorAsync<TK, TV, TCursor>(source, cursorFactory);

            // TODO #84
            // BaseCursorAsync<TK, TV, TCursor> inst;
            //if (!Pool.TryTake(out inst)) {
            //    inst = new BaseCursorAsync<TK, TV, TCursor>(source, cursorFactory);
            //}
            //inst._source = source;
            //inst._innerCursor = cursorFactory();
            //inst._disposed = false;
            //return inst;
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            _source = null;

            _innerCursor?.Dispose();
            _innerCursor = default(TCursor);

            _cancelledTcs = null;
            _registration.Dispose();
            _registration = default(CancellationTokenRegistration);

            _token = default(CancellationToken);

            // TODO #84
            //Pool.TryAdd(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            // sync move, hot path
            if (_innerCursor.MoveNext())
            {
                return TaskEx.TrueTask;
            }

            return MoveNextSlow(cancellationToken);
        }

        private Task<bool> MoveNextSlow(CancellationToken cancellationToken)
        {
            // we took a task, but it could have been created after the previous update, need to try moving next
            var task = _source.Updated;
            if (_innerCursor.MoveNext())
            {
                return TaskEx.TrueTask;
            }

            if (_source.IsReadOnly)
            { // false almost always
                return _innerCursor.MoveNext() ? TaskEx.TrueTask : TaskEx.FalseTask;
            }

            // now task will always be completed by NotifyUpdate

            Task<Task<bool>> returnTask = task.ContinueWith(continuationFunction: MoveNextContinuation,
                continuationOptions: TaskContinuationOptions.DenyChildAttach);

            if (!cancellationToken.CanBeCanceled)
            {
                return returnTask.Unwrap();
            }

            if (_token != cancellationToken)
            {
                _registration.Dispose();
                _token = cancellationToken;
                _cancelledTcs = new TaskCompletionSource<Task<bool>>();
                _registration = _token.Register(() =>
                {
                    _cancelledTcs.SetResult(TaskEx.FromCanceled<bool>(_token));
                });
            }

            var anyReturn = Task.WhenAny(returnTask, _cancelledTcs.Task);

            return anyReturn.Unwrap().Unwrap();
        }

        // TODO check if caching for this delegate is needed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task<bool> MoveNextContinuation(Task<bool> t)
        {
            if (!t.Result) return TaskEx.FalseTask;
            if (_token.IsCancellationRequested) return TaskEx.FromCanceled<bool>(_token);
            return _innerCursor.MoveNext() ? TaskEx.TrueTask : MoveNext(_token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return _innerCursor.MoveNext();
        }

        public void Reset()
        {
            _innerCursor?.Reset();
        }

        public KeyValuePair<TK, TV> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _innerCursor.Current; }
        }

        object IEnumerator.Current => ((IEnumerator)_innerCursor).Current;

        public KeyComparer<TK> Comparer => _innerCursor.Comparer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveAt(TK key, Lookup direction)
        {
            return _innerCursor.MoveAt(key, direction);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveFirst()
        {
            return _innerCursor.MoveFirst();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveLast()
        {
            return _innerCursor.MoveLast();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MovePrevious()
        {
            return _innerCursor.MovePrevious();
        }

        public TK CurrentKey
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _innerCursor.CurrentKey; }
        }

        public TV CurrentValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _innerCursor.CurrentValue; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<bool> MoveNextBatch(CancellationToken cancellationToken)
        {
            return _innerCursor.MoveNextBatch(cancellationToken);
        }

        public IReadOnlySeries<TK, TV> CurrentBatch
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _innerCursor.CurrentBatch; }
        }

        public IReadOnlySeries<TK, TV> Source => _innerCursor.Source;

        public bool IsContinuous => _innerCursor.IsContinuous;

        public ICursor<TK, TV> Clone()
        {
            return _innerCursor.Clone();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(TK key, out TV value)
        {
            return _innerCursor.TryGetValue(key, out value);
        }

        public void Dispose()
        {
            Dispose(true);
            //GC.SuppressFinalize(this);
        }

        // NB IS we did not called dispose explicitly, let the object die and do not try
        // to use finalizers
        //~BaseCursorAsync() {
        //    Dispose(false);
        //}
    }
}