using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AsyncHelpers
{
    public static class ForkJoin
    {
        public static R Invoke<T, R>(Func<R, T, R> reduce, Func<R> seedInit, params Func<T>[] operations)
        {
            var tasks = (from op in operations
                         select Task.Run(op)).ToArray();
            Task.WhenAll(tasks);
            return tasks.Select(t => t.Result).Aggregate(seedInit(), reduce);
        }

        public static R InvokeParChildRelationship<T, R>(Func<R, T, R> reduce, Func<R> seedInit, params Func<T>[] operations)
        {
            var results = new T[operations.Length];
            var task = Task.Run(() =>
            {
                for (int i = 0; i < operations.Length; i++)
                {
                    int index = i;
                    Task.Factory.StartNew(() => results[index] = operations[index](), TaskCreationOptions.AttachedToParent);
                }
            });
            Task.WhenAny(task);
            return results.Aggregate(seedInit(), reduce);
        }

        public static R InvokeParallelLoop<T, R>(Func<R, T, R> reduce, Func<R> seedInit, params Func<T>[] operations)
        {
            var results = new T[operations.Length];
            Parallel.For(0, operations.Length, i => results[i] = operations[i]());
            return results.Aggregate(seedInit(), reduce);
        }

        public static R InvokePLINQ<T, R>(Func<R, T, R> reduce, Func<R> seedInit, params Func<T>[] operations)
        {
            return operations.AsParallel().Select(f => f()).Aggregate(seedInit(), reduce);
        }
    }
}
