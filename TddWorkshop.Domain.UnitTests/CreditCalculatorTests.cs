using System;
using System.Collections;
using System.Collections.Generic;
using AutoFixture.Xunit2;
using Bogus;
using FsCheck.Xunit;
using TddWorkshop.Domain.InstantCredit;
using TddWorkshop.Domain.Tests.Arbitraries;
using TddWorkshop.Domain.Tests.Extensions;
using Xunit;
using static TddWorkshop.Domain.InstantCredit.CreditGoal;
using static TddWorkshop.Domain.InstantCredit.Employment;

namespace TddWorkshop.Domain.Tests;

public class CreditCalculatorTests
{
    [Theory, ClassData(typeof(CreditCalculatorTestData))]
    public void Calculate_PointsCalculatedCorrectly(CalculateCreditRequest request, bool hasCriminalRecord,
        int points)
    {
        var res = CreditCalculator.Calculate(request, hasCriminalRecord);
        Assert.Equal(points, res.Points);
    }

    [Theory, AutoData]
    public void Calculate_Autofixture_PercentsCalculatedCorrectly(CalculateCreditRequest request, bool hasCriminalRecord)
    {
        var res = CreditCalculator.Calculate(request, hasCriminalRecord);
        Assert.Equal(res.Points.ToInterestRate(), res.InterestRate);
    }

    [Fact]
    public void Calculate_WrongEnumEmployment_ThrowsArgumentOutOfRangeException()
    {
        var empl = (Employment)(-1);
        var faker = new Faker();
        var request = new CalculateCreditRequest(
            new PersonalInfo(33, faker.Person.FirstName, faker.Person.LastName),
            new CreditInfo(ConsumerCredit, 2000000, Deposit.Guarantor, empl, false),
            new PassportInfo("1234", "123456", faker.Date.Past(), faker.Company.CompanyName())
        );


        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreditCalculator.Calculate(request, false)
        );
    }

    [Property(Arbitrary = new[] {typeof(PostiveArbitraries)})]
    public bool Calculate_FsCheck_PercentsCalculatedCorrectly(CalculateCreditRequest request, bool hasCriminalRecord)
    {
        var res = CreditCalculator.Calculate(request, hasCriminalRecord);
        return res.Points.ToInterestRate() == res.InterestRate;
    }

    [Property(Arbitrary = new[] { typeof(PostiveArbitraries) })]
    public bool Response_FsCheck_PercentsCalculatedCorrectly(CalculateCreditResponse response)
    {
        return response.Points.ToInterestRate() == response.InterestRate;
    }

    [Property(Arbitrary = new[] { typeof(PostiveArbitraries) })]
    public bool Response_FsCheck_Greater80_IsSatisfied(CalculateCreditResponse response)
    {
        return response.IsApproved == response.Points > 80;
    }
}

public class CreditCalculatorTestData : IEnumerable<object[]>
{
    public static readonly CalculateCreditRequest Maximum =
        CreateRequest(30, ConsumerCredit, 1_000_001, Deposit.RealEstate, Employee, false);

    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] // 100 points - 12,5%
            { Maximum, false, 100 };

        yield return new object[] // 85 points - 26%
            { CreateRequest(30, ConsumerCredit, 1_000_001, Deposit.RealEstate, Employee, false), true, 85 };

        yield return new object[] // 16 points
            { CreateRequest(21, RealEstate, 5_000_001, Deposit.None, Unemployed, true), true, 16 };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static CalculateCreditRequest CreateRequest(int age, CreditGoal goal, decimal sum,
        Deposit deposit, Employment employment, bool hasOtherCredits)
    {
        var faker = new Faker();
        return new CalculateCreditRequest(
            new PersonalInfo(age, faker.Person.FirstName, faker.Person.LastName),
            new CreditInfo(goal, sum, deposit, employment, hasOtherCredits),
            new PassportInfo("1234", "123456", faker.Date.Past(), faker.Company.CompanyName())
        );
    }
}