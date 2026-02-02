using AudioStore.Common;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace AudioStore.Tests.Helpers;

/// <summary>
/// Custom FluentAssertions extensions for domain-specific assertions
/// </summary>
public static class CustomAssertions
{
    /// <summary>
    /// Asserts that a Result is successful
    /// </summary>
    public static ResultAssertions Should(this Result result)
    {
        return new ResultAssertions(result, AssertionChain.GetOrCreate());
    }

    /// <summary>
    /// Asserts that a Result<T> is successful
    /// </summary>
    public static ResultAssertions<T> Should<T>(this Result<T> result)
    {
        return new ResultAssertions<T>(result, AssertionChain.GetOrCreate());
    }
}

/// <summary>
/// Assertions for Result type
/// </summary>
public class ResultAssertions : ReferenceTypeAssertions<Result, ResultAssertions>
{
    public ResultAssertions(Result instance, AssertionChain assertionChain) 
        : base(instance, assertionChain)
    {
    }

    protected override string Identifier => "result";

    /// <summary>
    /// Asserts that the result is successful
    /// </summary>
    public AndConstraint<ResultAssertions> BeSuccess(string because = "", params object[] becauseArgs)
    {
        Subject.IsSuccess.Should().BeTrue(because, becauseArgs);
        return new AndConstraint<ResultAssertions>(this);
    }

    /// <summary>
    /// Asserts that the result is a failure
    /// </summary>
    public AndConstraint<ResultAssertions> BeFailure(string because = "", params object[] becauseArgs)
    {
        Subject.IsSuccess.Should().BeFalse(because, becauseArgs);
        return new AndConstraint<ResultAssertions>(this);
    }

    /// <summary>
    /// Asserts that the result has a specific error code
    /// </summary>
    public AndConstraint<ResultAssertions> HaveErrorCode(string expectedCode, string because = "", params object[] becauseArgs)
    {
        Subject.ErrorCode.Should().Be(expectedCode, because, becauseArgs);
        return new AndConstraint<ResultAssertions>(this);
    }

    /// <summary>
    /// Asserts that the result has a specific error message
    /// </summary>
    public AndConstraint<ResultAssertions> HaveErrorMessage(string expectedMessage, string because = "", params object[] becauseArgs)
    {
        Subject.Error.Should().Be(expectedMessage, because, becauseArgs);
        return new AndConstraint<ResultAssertions>(this);
    }

    /// <summary>
    /// Asserts that the result has a specific status code
    /// </summary>
    public AndConstraint<ResultAssertions> HaveStatusCode(int expectedStatusCode, string because = "", params object[] becauseArgs)
    {
        Subject.StatusCode.Should().Be(expectedStatusCode, because, becauseArgs);
        return new AndConstraint<ResultAssertions>(this);
    }

    /// <summary>
    /// Asserts that the result contains an error message
    /// </summary>
    public AndConstraint<ResultAssertions> ContainErrorMessage(string substring, string because = "", params object[] becauseArgs)
    {
        Subject.Error.Should().Contain(substring, because, becauseArgs);
        return new AndConstraint<ResultAssertions>(this);
    }
}

/// <summary>
/// Assertions for Result<T> type
/// </summary>
public class ResultAssertions<T> : ReferenceTypeAssertions<Result<T>, ResultAssertions<T>>
{
    public ResultAssertions(Result<T> instance, AssertionChain assertionChain) 
        : base(instance, assertionChain)
    {
    }

    protected override string Identifier => "result";

    /// <summary>
    /// Asserts that the result is successful
    /// </summary>
    public AndConstraint<ResultAssertions<T>> BeSuccess(string because = "", params object[] becauseArgs)
    {
        Subject.IsSuccess.Should().BeTrue(because, becauseArgs);
        return new AndConstraint<ResultAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the result is a failure
    /// </summary>
    public AndConstraint<ResultAssertions<T>> BeFailure(string because = "", params object[] becauseArgs)
    {
        Subject.IsSuccess.Should().BeFalse(because, becauseArgs);
        return new AndConstraint<ResultAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the result has a specific error code
    /// </summary>
    public AndConstraint<ResultAssertions<T>> HaveErrorCode(string expectedCode, string because = "", params object[] becauseArgs)
    {
        Subject.ErrorCode.Should().Be(expectedCode, because, becauseArgs);
        return new AndConstraint<ResultAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the result has a specific error message
    /// </summary>
    public AndConstraint<ResultAssertions<T>> HaveErrorMessage(string expectedMessage, string because = "", params object[] becauseArgs)
    {
        Subject.Error.Should().Be(expectedMessage, because, becauseArgs);
        return new AndConstraint<ResultAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the result has a specific status code
    /// </summary>
    public AndConstraint<ResultAssertions<T>> HaveStatusCode(int expectedStatusCode, string because = "", params object[] becauseArgs)
    {
        Subject.StatusCode.Should().Be(expectedStatusCode, because, becauseArgs);
        return new AndConstraint<ResultAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the result data is not null
    /// </summary>
    public AndConstraint<ResultAssertions<T>> HaveData(string because = "", params object[] becauseArgs)
    {
        Subject.Value.Should().NotBeNull(because, becauseArgs);
        return new AndConstraint<ResultAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the result data matches the expected value
    /// </summary>
    public AndConstraint<ResultAssertions<T>> HaveData(T expectedData, string because = "", params object[] becauseArgs)
    {
        Subject.Value.Should().BeEquivalentTo(expectedData, because, becauseArgs);
        return new AndConstraint<ResultAssertions<T>>(this);
    }

    /// <summary>
    /// Asserts that the result data satisfies a predicate
    /// </summary>
    public AndConstraint<ResultAssertions<T>> HaveDataMatching(Func<T, bool> predicate, string because = "", params object[] becauseArgs)
    {
        Subject.Value.Should().Match<T>(data => predicate(data), because, becauseArgs);
        return new AndConstraint<ResultAssertions<T>>(this);
    }

    /// <summary>
    /// Returns the data for further assertions
    /// </summary>
    public T WhichData()
    {
        Subject.Value.Should().NotBeNull();
        return Subject.Value!;
    }
}

