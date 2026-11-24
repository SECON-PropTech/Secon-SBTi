using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using SeconSbtiBackend.DTOs;

namespace tests.DTOsTests;

public class SoldProductDtosTests
{
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();

    /// <summary>
    /// Test helper: runs the same validation rules as the API and returns the validation results.
    /// </summary>
    private static List<ValidationResult> Validate(UpdateSoldProductRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    private static SoldProductResponse CreateResponse() => new(
        ProductId, "ART-1", "Fläkt A", GroupId, "Fläktar",
        50m, 10m, "measured", "Uppmätt");

    /************************UpdateSoldProductRequest: values************************/
    [Fact]
    public void UpdateSoldProductRequest_stores_all_values()
    {
        var request = new UpdateSoldProductRequest
        {
            groupId = GroupId,
            KwhPerYear = 50m,
            LifetimeYears = 10m,
            MeasurementQuality = "measured",
            Comment = "Uppmätt 2025"
        };

        request.groupId.Should().Be(GroupId);
        request.KwhPerYear.Should().Be(50m);
        request.LifetimeYears.Should().Be(10m);
        request.MeasurementQuality.Should().Be("measured");
        request.Comment.Should().Be("Uppmätt 2025");
    }

    [Fact]
    public void UpdateSoldProductRequest_optional_values_default_to_null()
    {
        var request = new UpdateSoldProductRequest { MeasurementQuality = "estimated" };

        request.groupId.Should().BeNull();
        request.KwhPerYear.Should().BeNull();
        request.LifetimeYears.Should().BeNull();
        request.Comment.Should().BeNull();
    }

    [Fact]
    public void UpdateSoldProductRequest_with_expression_copies_the_record_and_changes_one_value()
    {
        var original = new UpdateSoldProductRequest { MeasurementQuality = "estimated", KwhPerYear = 5m };
        var copy = original with { MeasurementQuality = "measured" };

        copy.MeasurementQuality.Should().Be("measured");
        copy.KwhPerYear.Should().Be(5m);
        original.MeasurementQuality.Should().Be("estimated");
    }

    [Fact]
    public void UpdateSoldProductRequest_is_equal_when_all_values_are_equal()
    {
        var first = new UpdateSoldProductRequest { groupId = GroupId, KwhPerYear = 1m, MeasurementQuality = "measured" };
        var second = new UpdateSoldProductRequest { groupId = GroupId, KwhPerYear = 1m, MeasurementQuality = "measured" };

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void UpdateSoldProductRequest_is_not_equal_when_a_value_differs()
    {
        var first = new UpdateSoldProductRequest { MeasurementQuality = "measured" };
        var second = first with { Comment = "Annan" };

        first.Should().NotBe(second);
        (first == second).Should().BeFalse();
    }

    [Fact]
    public void UpdateSoldProductRequest_ToString_contains_type_name_and_values()
    {
        var text = new UpdateSoldProductRequest { MeasurementQuality = "measured", Comment = "Hej" }.ToString();

        text.Should().Contain(nameof(UpdateSoldProductRequest));
        text.Should().Contain("measured");
        text.Should().Contain("Hej");
    }

    /************************UpdateSoldProductRequest: validation boundaries************************/
    [Fact]
    public void UpdateSoldProductRequest_valid_request_with_all_values_has_no_errors()
    {
        Validate(new UpdateSoldProductRequest
        {
            groupId = GroupId,
            KwhPerYear = 50m,
            LifetimeYears = 10m,
            MeasurementQuality = "measured",
            Comment = "Kommentar"
        }).Should().BeEmpty();
    }

    [Fact]
    public void UpdateSoldProductRequest_maximum_kwh_is_allowed()
    {
        Validate(new UpdateSoldProductRequest { KwhPerYear = 1_000_000_000m, MeasurementQuality = "estimated" }).Should().BeEmpty();
    }

    [Fact]
    public void UpdateSoldProductRequest_maximum_lifetime_is_allowed()
    {
        Validate(new UpdateSoldProductRequest { LifetimeYears = 1_000m, MeasurementQuality = "estimated" }).Should().BeEmpty();
    }

    [Fact]
    public void UpdateSoldProductRequest_smallest_positive_lifetime_is_allowed()
    {
        Validate(new UpdateSoldProductRequest { LifetimeYears = 0.01m, MeasurementQuality = "estimated" }).Should().BeEmpty();
    }

    [Theory]
    [InlineData("measured")]
    [InlineData("estimated")]
    public void UpdateSoldProductRequest_accepts_supported_measurement_quality(string quality)
    {
        Validate(new UpdateSoldProductRequest { MeasurementQuality = quality }).Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Measured")]
    [InlineData("unknown")]
    public void UpdateSoldProductRequest_rejects_unsupported_measurement_quality(string quality)
    {
        Validate(new UpdateSoldProductRequest { MeasurementQuality = quality }).SelectMany(result => result.MemberNames)
            .Should().Equal(nameof(UpdateSoldProductRequest.MeasurementQuality));
    }

    [Fact]
    public void UpdateSoldProductRequest_reports_every_invalid_field()
    {
        Validate(new UpdateSoldProductRequest
        {
            KwhPerYear = -1m,
            LifetimeYears = 0m,
            MeasurementQuality = "guessed"
        })
            .SelectMany(result => result.MemberNames)
            .Should().BeEquivalentTo(
                nameof(UpdateSoldProductRequest.KwhPerYear),
                nameof(UpdateSoldProductRequest.LifetimeYears),
                nameof(UpdateSoldProductRequest.MeasurementQuality));
    }

    /************************SoldProductResponse************************/
    [Fact]
    public void SoldProductResponse_stores_all_values()
    {
        var response = CreateResponse();

        response.Id.Should().Be(ProductId);
        response.ArticleNo.Should().Be("ART-1");
        response.Name.Should().Be("Fläkt A");
        response.GroupId.Should().Be(GroupId);
        response.GroupName.Should().Be("Fläktar");
        response.KwhPerYear.Should().Be(50m);
        response.LifetimeYears.Should().Be(10m);
        response.MeasurementQuality.Should().Be("measured");
        response.Comment.Should().Be("Uppmätt");
    }

    [Fact]
    public void SoldProductResponse_allows_null_for_values_that_are_not_set()
    {
        var response = new SoldProductResponse(
            ProductId, null, "Fläkt A", null, 
            null, null, null, "estimated", null);

        response.ArticleNo.Should().BeNull();
        response.GroupId.Should().BeNull();
        response.GroupName.Should().BeNull();
        response.KwhPerYear.Should().BeNull();
        response.LifetimeYears.Should().BeNull();
        response.Comment.Should().BeNull();
    }

    [Fact]
    public void SoldProductResponse_keeps_zero_kwh_apart_from_null()
    {
        var response = CreateResponse() with { KwhPerYear = 0m };
        response.KwhPerYear.Should().Be(0m);
        response.KwhPerYear.Should().NotBeNull();
    }

    [Fact]
    public void SoldProductResponse_is_equal_when_all_values_are_equal()
    {
        var first = CreateResponse();
        var second = CreateResponse();

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        (first != second).Should().BeFalse();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void SoldProductResponse_is_not_equal_when_a_value_differs()
    {
        var first = CreateResponse();
        var second = first with { Name = "Fläkt B" };

        first.Should().NotBe(second);
        (first == second).Should().BeFalse();
        second.Name.Should().Be("Fläkt B");
    }

    [Fact]
    public void SoldProductResponse_ToString_contains_type_name_and_values()
    {
        var text = CreateResponse().ToString();
        text.Should().Contain(nameof(SoldProductResponse));
        text.Should().Contain("Fläkt A");
        text.Should().Contain("ART-1");
    }

    [Fact]
    public void SoldProductResponse_can_be_deconstructed()
    {
        var (id, articleNo, name, groupId, groupName, kwh, lifetime, quality, comment) = CreateResponse();

        id.Should().Be(ProductId);
        articleNo.Should().Be("ART-1");
        name.Should().Be("Fläkt A");
        groupId.Should().Be(GroupId);
        groupName.Should().Be("Fläktar");
        kwh.Should().Be(50m);
        lifetime.Should().Be(10m);
        quality.Should().Be("measured");
        comment.Should().Be("Uppmätt");
    }
}