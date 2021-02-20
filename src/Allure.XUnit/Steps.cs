using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Allure.Commons;
using HeyRed.Mime;
using static Allure.Xunit.AllureXunitHelper;

namespace Allure.Xunit
{
    public static class Steps
    {
        public static ExecutableItem Current => AllureXunitHelper.Steps.Value.LastOrDefault();

        public static Task<T> Step<T>(string name, Func<Task<T>> action)
        {
            StartStep(name);
            return Execute(action);
        }

        private static async Task<T> Execute<T>(Func<Task<T>> action)
        {
            T result;
            try
            {
                result = await action();
            }
            catch (Exception)
            {
                FailStep();
                throw;
            }

            PassStep();
            return result;
        }

        private static T Execute<T>(Func<T> action)
        {
            T result;
            try
            {
                result = action();
            }
            catch (Exception)
            {
                FailStep();
                throw;
            }

            PassStep();
            return result;
        }

        public static T Step<T>(string name, Func<T> action)
        {
            StartStep(name);
            return Execute(action);
        }

        public static void Step(string name, Action action) =>
            Step(name, (Func<object>) (() =>
            {
                action();
                return null;
            }));

        public static Task Step(string name, Func<Task> action) =>
            Step(name, async () =>
            {
                await action();
                return Task.FromResult<object>(null);
            });

        public static void Step(string name) =>
            Step(name, () => { });

        public static Task<T> Before<T>(string name, Func<Task<T>> action)
        {
            StartBeforeFixture(name);
            return Execute(action);
        }

        public static T Before<T>(string name, Func<T> action)
        {
            StartBeforeFixture(name);
            return Execute(action);
        }

        public static void Before(string name, Action action) =>
            Before(name, (Func<object>) (() =>
            {
                action();
                return null;
            }));

        public static Task Before(string name, Func<Task> action) =>
            Before(name, async () =>
            {
                await action();
                return Task.FromResult<object>(null);
            });

        public static Task<T> After<T>(string name, Func<Task<T>> action)
        {
            StartAfterFixture(name);
            return Execute(action);
        }

        public static T After<T>(string name, Func<T> action)
        {
            StartAfterFixture(name);
            return Execute(action);
        }

        public static void After(string name, Action action) =>
            After(name, (Func<object>) (() =>
            {
                action();
                return null;
            }));

        public static Task After(string name, Func<Task> action) =>
            After(name, async () =>
            {
                await action();
                return Task.FromResult<object>(null);
            });
    }
}