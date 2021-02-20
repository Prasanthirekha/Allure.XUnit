using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Allure.Commons;
using Allure.XUnit;
using Allure.Xunit.Attributes;
using Xunit.Abstractions;

namespace Allure.Xunit
{
    public class AllureXunitHelper
    {
        internal static readonly AsyncLocal<List<ExecutableItem>> Steps = new();
        private static readonly AsyncLocal<TestResultContainer> TestResultContainer = new();
        private static readonly AsyncLocal<TestResult> TestResult = new();

        static AllureXunitHelper()
        {
            const string allureConfigEnvVariable = "ALLURE_CONFIG";
            const string allureConfigName = "allureConfig.json";

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(allureConfigEnvVariable)))
            {
                return;
            }

            var allureConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, allureConfigName);

            Environment.SetEnvironmentVariable(allureConfigEnvVariable, allureConfigPath);
        }

        public static void StartTestCase(ITestCaseStarting testCaseStarting)
        {
            StartTestContainer(testCaseStarting.TestClass);
            var testCase = testCaseStarting.TestCase;
            var uuid = NewUuid(testCase.DisplayName);
            TestResult.Value = new()
            {
                uuid = uuid,
                name = testCase.DisplayName,
                historyId = testCase.DisplayName,
                fullName = testCase.DisplayName,
                labels = new()
                {
                    Label.Thread(),
                    Label.Host(),
                    Label.TestClass(testCase.TestMethod.TestClass.Class.Name),
                    Label.TestMethod(testCase.DisplayName),
                    Label.Package(testCase.TestMethod.TestClass.Class.Name)
                }
            };
            UpdateTestDataFromAttributes(TestResult.Value, testCase);
            AllureLifecycle.Instance.StartTestCase(TestResultContainer.Value.uuid, TestResult.Value);
        }

        public static void MarkTestCaseAsFailed(ITestFailed testFailed)
        {
            var statusDetails = TestResult.Value.statusDetails ??= new();
            statusDetails.trace = string.Join('\n', testFailed.StackTraces);
            statusDetails.message = string.Join('\n', testFailed.Messages);
            TestResult.Value.status = Status.failed;
        }

        public static void MarkTestCaseAsPassed(ITestPassed testPassed)
        {
            var statusDetails = TestResult.Value.statusDetails ??= new();
            statusDetails.message = testPassed.Output;
            TestResult.Value.status = Status.passed;
        }

        public static void FinishTestCase()
        {
            AllureLifecycle.Instance.StopTestCase(TestResult.Value.uuid);
            AllureLifecycle.Instance.WriteTestCase(TestResult.Value.uuid);
            AllureLifecycle.Instance.StopTestContainer(TestResultContainer.Value.uuid);
            AllureLifecycle.Instance.WriteTestContainer(TestResultContainer.Value.uuid);
        }

        public static void StartBeforeFixture(string name)
        {
            var fixtureResult = new FixtureResult
            {
                name = name,
                stage = Stage.running,
                start = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };

            TestResultContainer.Value.befores ??= new();
            TestResultContainer.Value.befores.Add(fixtureResult);
            Steps.Value.Add(fixtureResult);
            Log($"Started Before: {name}");
        }

        public static void StartAfterFixture(string name)
        {
            var fixtureResult = new FixtureResult
            {
                name = name,
                stage = Stage.running,
                start = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };

            TestResultContainer.Value.afters ??= new();
            TestResultContainer.Value.afters.Add(fixtureResult);
            Steps.Value.Add(fixtureResult);
            Log($"Started After: {name}");
        }

        public static void StartStep(string name)
        {
            var stepResult = new StepResult
            {
                name = name,
                stage = Stage.running,
                start = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };
            var parent = Steps.Value.LastOrDefault();
            if (parent == null)
            {
                parent = TestResult.Value;
            }

            parent.steps ??= new();
            parent.steps.Add(stepResult);
            Steps.Value.Add(stepResult);
            Log($"Started Step: {name}");
        }

        public static void PassStep()
        {
            PassStep(Steps.Value.Last());
        }

        public static void PassStep(ExecutableItem step)
        {
            Steps.Value.Remove(step);
            step.stage = Stage.finished;
            step.stop = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            step.status = Status.passed;
            Log("Passed");
        }

        public static void FailStep()
        {
            FailStep(Steps.Value.Last());
        }

        public static void FailStep(ExecutableItem step)
        {
            Steps.Value.Remove(step);
            step.stage = Stage.finished;
            step.stop = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            step.status = Status.failed;
            Log("Failed");
        }

        public static async Task AddAttachment(string name, string type, byte[] content, string fileExtension)
        {
            var source = $"{Guid.NewGuid():N}{AllureConstants.ATTACHMENT_FILE_SUFFIX}{fileExtension}";
            await File.WriteAllBytesAsync(Path.Combine(AllureLifecycle.Instance.ResultsDirectory, source), content);
            List<Attachment> attachments;
            if (Steps.Value.Any())
            {
                attachments = Steps.Value.Last().attachments ??= new();
            }
            else
            {
                attachments = TestResult.Value.attachments ??= new();
            }
            attachments.Add(new()
            {
                name = name,
                type = type,
                source = source
            });
        }

        private static void StartTestContainer(ITestClass testClass)
        {
            var uuid = NewUuid(testClass.Class.Name);
            TestResultContainer.Value = new()
            {
                uuid = uuid,
                name = testClass.Class.Name
            };
            AllureLifecycle.Instance.StartTestContainer(TestResultContainer.Value);
        }

        private static string NewUuid(string name)
        {
            var uuid = string.Concat(Guid.NewGuid().ToString(), "-", name);
            return uuid;
        }

        private static void Log(string message)
        {
            AllureMessageBus.TestOutputHelper.Value.WriteLine("╬════════════════════════");
            AllureMessageBus.TestOutputHelper.Value.WriteLine($"║ {message}");
            AllureMessageBus.TestOutputHelper.Value.WriteLine("╬═══════════════");
        }

        private static void UpdateTestDataFromAttributes(TestResult testResult, ITestCase testCase)
        {
            var attributes = testCase.TestMethod.Method.GetCustomAttributes(typeof(IAllureInfo));

            foreach (var attribute in attributes)
            {
                switch (((IReflectionAttributeInfo) attribute).Attribute)
                {
                    case AllureFeatureAttribute featureAttribute:
                        foreach (var feature in featureAttribute.Features)
                        {
                            testResult.labels.Add(Label.Feature(feature));
                        }

                        break;

                    case AllureLinkAttribute linkAttribute:
                        testResult.links.Add(linkAttribute.Link);
                        break;

                    case AllureIssueAttribute issueAttribute:
                        testResult.links.Add(issueAttribute.IssueLink);
                        break;

                    case AllureOwnerAttribute ownerAttribute:
                        testResult.labels.Add(Label.Owner(ownerAttribute.Owner));
                        break;

                    case AllureSuiteAttribute suiteAttribute:
                        testResult.labels.Add(Label.Suite(suiteAttribute.Suite));
                        break;

                    case AllureSubSuiteAttribute subSuiteAttribute:
                        testResult.labels.Add(Label.SubSuite(subSuiteAttribute.SubSuite));
                        break;

                    case AllureEpicAttribute epicAttribute:
                        testResult.labels.Add(Label.Epic(epicAttribute.Epic));
                        break;

                    case AllureTagAttribute tagAttribute:
                        foreach (var tag in tagAttribute.Tags)
                        {
                            testResult.labels.Add(Label.Tag(tag));
                        }

                        break;

                    case AllureSeverityAttribute severityAttribute:
                        testResult.labels.Add(Label.Severity(severityAttribute.Severity));
                        break;

                    case AllureParentSuiteAttribute parentSuiteAttribute:
                        testResult.labels.Add(Label.ParentSuite(parentSuiteAttribute.ParentSuite));
                        break;

                    case AllureStoryAttribute storyAttribute:
                        foreach (var story in storyAttribute.Stories)
                        {
                            testResult.labels.Add(Label.Story(story));
                        }

                        break;

                    case AllureDescriptionAttribute descriptionAttribute:
                        testResult.description = descriptionAttribute.Description;
                        break;

                    case AllureLabelAttribute labelAttribute:
                        testResult.labels.Add(new()
                        {
                            name = labelAttribute.Label,
                            value = labelAttribute.Value
                        });
                        break;
                }
            }
        }
    }
}