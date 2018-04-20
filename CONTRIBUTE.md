# Contribute

Rebus.AwsSnsAndSqs is an [inner source](https://en.wikipedia.org/wiki/Inner_source) project. Pull requests are welcomed from anyone in Cox Automotive.

I do prefer it if we communicate a little bit before you send PRs, though. This is because I value your time, and it would be a shame if you spent time working on something that could be made better in another way, or wasn't actually needed because what you wanted to achieve could be done better in another way, etc.

## "Beginners"
Contributions are ALSO very welcome if you consider yourself a beginner at [inner source](https://en.wikipedia.org/wiki/Inner_source). Everyone has to start somewhere, right?

Here's how you would ideally do it if want to contribute:

Pick an [issue](https://ghe.coxautoinc.com/Mike-Connelly/Rebus.AwsSnsAndSqs/issues) you're interested in doing, or dream up something yourself that you feel is missing.
If you talk to me first (either via comments on the issue or by email), I will guarantee that your contribution is accepted.
Send me a "pull request" (which is how you make contributions on GitHub)

## Quick Start

1. Fork this repository ([Fork A Repo @ GitHub docs](https://help.github.com/articles/fork-a-repo/))
2. Clone the fork ([Cloning A Repository @ GitHub docs](https://help.github.com/articles/cloning-a-repository/))
3. You will need a suffient credentials to create sns and sqs resources. See [this](https://aws.amazon.com/blogs/developer/referencing-credentials-using-profiles/) for setting up an aws profile on your local machine. By default the [FallbackCredentialsFactory](https://github.com/aws/aws-sdk-net/blob/master/sdk/src/Core/Amazon.Runtime/Credentials/FallbackCredentialsFactory.cs) is used for finding credentials to run the tests and the example.
4. Open solution [Rebus.AwsSnsAndSqs](Rebus.AwsSnsAndSqs.sln)
5. Push your local changes to your fork ([Pushing To A Remote @ GitHub docs](https://help.github.com/articles/pushing-to-a-remote/))
6. Send me a pull request ([Using Pull Requests @ GitHub docs](https://help.github.com/articles/using-pull-requests/))

When this is done, the changes become visible. They can then be reviewed and discussed.

If additional changes are pushed to the fork during this process, the changes become immediately available in the pull request.

All PRs are accepted by the act of being merged in.

## Versioning
Version numbers are controlled via the [Jenkins file](https://ghe.coxautoinc.com/Mike-Connelly/Rebus.AwsSnsAndSqs/blob/master/Jenkinsfile)

We are using (semantic versioning)[https://semver.org/#semantic-versioning-200]

Given a version number MAJOR.MINOR.PATCH, increment the:

1. MAJOR version when you make incompatible API changes,
2. MINOR version when you add functionality in a backwards-compatible manner, and
3. PATCH version when you make backwards-compatible bug fixes. (The build will set this based on the build number for the branch)
Additional labels we be applied based on the source branch.

The Major version should match the major version of rebus being used.

## Testing changes
Testing is done using Nunit with Rebus.Tests.Contracts providing supporting mocks and fakes for testing.

Example test
```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AwsSnsAndSqs.Config;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

#pragma warning disable 1998

namespace Rebus.AwsSnsAndSqsTests
{
    [TestFixture, Category("snsAndSqsPubSub")]
    public class SnsAndSqsPubSubTest : FixtureBase
    {
        readonly string _publisherQueueName = TestConfig.GetName("publisher");
        readonly string _subscriber1QueueName = TestConfig.GetName("sub1");
        readonly string _subscriber2QueueName = TestConfig.GetName("sub2");
        BuiltinHandlerActivator _publisher;

        protected override void SetUp()
        {
            _publisher = GetBus(_publisherQueueName);
        }

        [Test]
        public async Task PubSubTest()
        {
            var sub1GotEvent = new ManualResetEvent(false);
            var sub2GotEvent = new ManualResetEvent(false);

            var sub1 = GetBus(_subscriber1QueueName, async str =>
            {
                if (str == "weehoo!!")
                {
                    sub1GotEvent.Set();
                }
            });

            var sub2 = GetBus(_subscriber2QueueName, async str =>
            {
                if (str == "weehoo!!")
                {
                    sub2GotEvent.Set();
                }
            });

            await sub1.Bus.Subscribe<string>();
            await sub2.Bus.Subscribe<string>();

            await _publisher.Bus.Publish("weehoo!!");

            sub1GotEvent.WaitOrDie(TimeSpan.FromSeconds(30));
            sub2GotEvent.WaitOrDie(TimeSpan.FromSeconds(30));
        }

        BuiltinHandlerActivator GetBus(string queueName, Func<string, Task> handlerMethod = null)
        {
            var activator = Using(new BuiltinHandlerActivator());

            if (handlerMethod != null)
            {
                activator.Handle(handlerMethod);
            }

            Configure.With(activator)
                .Transport(t =>
                {
                    t.UseAmazonSnsAndSqs(workerQueueAddress: queueName);
                })
                .Routing(r => r.TypeBased().Map<string>(queueName))
                .Start();

            return activator;
        }
    }
}
```
## C# Standards 
Not all the existing code keeps to the standard. Much of the library was ported from the [Rebus.AmazonSQS](https://github.com/rebus-org/Rebus.AmazonSQS) repository and will be refactored over time.

### Private members
All private member variables should be preceded with an underscore then camel casing and placed at the top of the class

_Example_
```csharp
private string _myString;
```

Avoid using private methods

### Types, Methods, Constants and Properties
Use Pascal Casing
```csharp
public class MyClass{}
public string MyMethod(){}
public string MyProperty{ get; set; }
public const string MyConstant = "constantly this";
```

Avoid methods with more than 200 lines of code

Avoid methods with more than 5 arguments. Use structures for passing multiple arguments

Do not manually edit machine generated code

Avoid comments that explain the obvious. Code should be self explanatory with readable and descriptive method and variable names.

Mark public and protected methods as virtual.

Do not provide public or protected member variables use Properties instead.

Avoid using private methods

Avoid methods with a cyclomatic complexity of greater than 12

Prefer using statements over fully qualified names

Avoid Fully Qualified Names
```csharp
//Avoid
Vin.Engine.Engagement.Calculation.PaymentCalculator calculator = new Vin.Engine.Engagement.Calculation.PaymentCalculator();
  
//Correct
using Vin.Engine.Calculation.PaymentCalculator;
public class MyClass
{
    PaymentCalculator calculator = new PaymentCalculator();
}
```

### Local Variables and arguments

Use Camel Casing
```csharp
int myNumber = 0;
```
### Interfaces
Interfaces Begin with the letter I
```csharp
public interface IMyInterface
{}
```
Avoid Interfaces with one Method try to have 3 - 5 methods per interface no more than 20, but 12 is probably the practical limit. 

### Attributes, Exceptions, Base Classes
Suffixed with the word Attribute, Exception, Base

Properly Suffix your classes
```csharp
public class MyAttribute : System.Attribute{}
public class MyException : System.Exception{}
public abstract class MyClassBase {}
```

### Generics
Use capital letters for types. Reserve suffixing type when dealing with .NET type Type

```csharp
//Avoid
public class LinkedList<KeyType,DataType>
{}
//Correct
public class LinkedList<T,K>
{}
```
Files
One class per file.  Do not put multiple classes in the same file. 

Avoid files with more than 500 lines of code excluding machine generated code

### Var Keyword
Only use var when the  right side of the assignment clearly indicates the type of variable.

Do not assign method return types or complex expressions into a var variable, with the exception of LINQ projections that result in an anonymous type.

Use the var keyword appropriately
```csharp
//Avoid
var myVariable = DoSomething();
//Correct
var name = EmployeeName;
```
### Lambda Expressions
Mimic the code layout of a regular method. Omit the variable type and rely on type inference, yet use parenthesis

Format Lambda's appropriately
```csharp
delegate void SomeDelegate(string someString);
  
SomeDelegate someDelegate = (name)
                            {
                                Trace.WriteLine(name);
                            }
```
### Ternary
Do not nest ternary operations

```csharp
//Avoid
int something = (value) ? someThing : (someOtherValue) ? someOtherThing : yetAnotherThing;
```